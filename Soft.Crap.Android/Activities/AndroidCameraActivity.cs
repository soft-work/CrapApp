using System;
using System.Collections.Generic;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Hardware;
using Android.Hardware.Camera2;
using Android.Hardware.Camera2.Params;
using Android.Media;
using Android.OS;
using Android.Util;
using Android.Views;

using Java.IO;
using Java.Lang;
using Java.Nio;

using Byte = Java.Lang.Byte;
using File = Java.IO.File;
using Math = System.Math;
using Object = Java.Lang.Object;
using CameraError = Android.Hardware.Camera2.CameraError;
using Thread = System.Threading.Thread;

// https://inducesmile.com/android/android-camera2-api-example-tutorial/
// (http://opencamera.sourceforge.net/ ?)
// https://developer.android.com/reference/android/hardware/Camera.Parameters.html#setRotation(int)

namespace Soft.Crap.Android.Activities
{
    public abstract class AndroidCameraActivity : Activity,
                                                  TextureView.ISurfaceTextureListener
    {
        private const int JpegQuality = 95;
        
        protected TextureView TextureView { set; get; }
        protected int? DeviceOrientation { private set; get; }
        protected abstract void OnReady();
        protected abstract void OnDone();

        private CameraDevice _cameraDevice;
        private OrientationEventListener _orientationListener;
        
        private Size _imageDimension;
        private Handler _backgroundHandler;
        private HandlerThread _backgroundThread;

        #region Activity Overrides

        override protected void OnCreate
        (
            Bundle bundle
        )
        {
            base.OnCreate(bundle);

            _orientationListener = new DeviceOrientationEventListener(this,
                                                                      SensorDelay.Normal,
                                                                      OrientationChanged);
            _orientationListener.Enable();
        }

        protected override void OnResume()
        {
            base.OnResume();

            _orientationListener.Enable();

            if (TextureView == null)
            {
                throw new InvalidOperationException(string.Format("'{0}' must be set within '{1}'",
                                                    nameof(TextureView),                                
                                                    nameof(OnCreate)));
            }

            // start background thread:
            _backgroundThread = new HandlerThread(nameof(AndroidCameraActivity));
            _backgroundThread.Start();
            _backgroundHandler = new Handler(_backgroundThread.Looper);

            if (TextureView.IsAvailable)
            {
                OpenCamera();
            }
            else
            {
                TextureView.SurfaceTextureListener = this;
            }
        }

        protected override void OnPause()
        {
            //CloseCamera();

            _orientationListener.Disable();

            // stop background thread:
            _backgroundThread.QuitSafely();
            _backgroundThread.Join();
            _backgroundThread = null;
            _backgroundHandler = null;

            base.OnPause();
        }

        protected override void OnDestroy()
        {
            _orientationListener.Disable();

            base.OnDestroy();
        }

        #endregion Activity Overrides

        #region Capture Picture

        protected void CapturePicture
        (
            string picturePath
        )
        { 
            if (_cameraDevice == null)
            {
                return;
            }

            if (DeviceOrientation == null)
            {
                throw new NullReferenceException(nameof(DeviceOrientation));
            }

            if (DeviceOrientation == OrientationEventListener.OrientationUnknown)
            {
                return;
            }

            var cameraManager = GetCameraManager();

            CameraCharacteristics cameraCharacteristics 
                = cameraManager.GetCameraCharacteristics(_cameraDevice.Id);
 
            Size[] jpegSizes = null;
            Size[] thumbnailSizes = null;

            int cameraOrientation = 0;

            if (cameraCharacteristics != null)
            {
                var configurationMap = (StreamConfigurationMap)cameraCharacteristics.Get
                (
                    CameraCharacteristics.ScalerStreamConfigurationMap
                );

                jpegSizes = configurationMap.GetHighResolutionOutputSizes
                (
                    (int)ImageFormatType.Jpeg
                );

                if ((jpegSizes == null) || (jpegSizes.Length == 0))
                {
                    jpegSizes = configurationMap.GetOutputSizes
                    (
                        (int)ImageFormatType.Jpeg
                    );
                }                

                thumbnailSizes = cameraCharacteristics.Get
                (
                    CameraCharacteristics.JpegAvailableThumbnailSizes
                )
                .ToArray<Size>();

                cameraOrientation = (int)cameraCharacteristics.Get
                (
                    CameraCharacteristics.SensorOrientation
                );
            }

            int jpegWidth = 640; // why exactly these default values?!            
            int jpegHeight = 480;            

            if ((jpegSizes != null) && (jpegSizes.Length > 0))
            {
                jpegWidth = jpegSizes[0].Width;
                jpegHeight = jpegSizes[0].Height;
            }

            ImageReader imageReader = ImageReader.NewInstance(jpegWidth,
                                                              jpegHeight,
                                                              ImageFormatType.Jpeg,
                                                              maxImages : 1);
            var outputSurfaces = new List<Surface>(2);
            outputSurfaces.Add(imageReader.Surface);
            outputSurfaces.Add(new Surface(TextureView.SurfaceTexture));

            CaptureRequest.Builder captureBuilder 
                = _cameraDevice.CreateCaptureRequest(CameraTemplate.StillCapture);

            captureBuilder.AddTarget(imageReader.Surface);
            // https://developer.android.com/reference/android/hardware/camera2/CameraMetadata.html#CONTROL_MODE_AUTO
            captureBuilder.Set(CaptureRequest.ControlMode,
                               1); // CONTROL_MODE_AUTO

            int pictureRotation = (DeviceOrientation.Value + cameraOrientation) % 360;

            captureBuilder.Set(CaptureRequest.JpegOrientation,
                               pictureRotation);            

            var jpegQuality = new Byte(JpegQuality);

            captureBuilder.Set(CaptureRequest.JpegQuality,
                               jpegQuality);

            captureBuilder.Set(CaptureRequest.JpegThumbnailQuality,
                               jpegQuality);

            var thumbnailSize = GetBestThumbnailSize(jpegWidth,
                                                     jpegHeight,
                                                     thumbnailSizes);

            captureBuilder.Set(CaptureRequest.JpegThumbnailSize,
                               thumbnailSize);

            var pictureFile = new File(picturePath);
            
            imageReader.SetOnImageAvailableListener(new ImageAvailableListener(pictureFile),
                                                    _backgroundHandler);

            var captureListener = new CameraCaptureSessionCallback(CreateCameraPreview,
                                                                   OnDone);
            _cameraDevice.CreateCaptureSession(outputSurfaces,
                                               new CameraCaptureStateCallback(captureBuilder,
                                                                              captureListener,
                                                                              _backgroundHandler),
                                               _backgroundHandler);
        }

        private Size GetBestThumbnailSize
        (
            int jpegWidth,
            int jpegHeight,
            Size[] thumbnailSizes
        )
        {
            var bestSize = new Size(0, 0); // no thumbnail

            if (thumbnailSizes == null)
            {
                return bestSize;
            }

            double jpegRatio = (double)jpegWidth / jpegHeight;

            foreach(Size thumbnailSize in thumbnailSizes)
            {
                if ((thumbnailSize.Width == 0) || (thumbnailSize.Height == 0))
                {
                    continue;
                }

                double thumbnailRatio = (double)thumbnailSize.Width / thumbnailSize.Height;

                if (Math.Abs(thumbnailRatio - jpegRatio) < double.Epsilon)
                {
                    bestSize = thumbnailSize;

                    break;
                }
            }

            return bestSize;
        }

        private void OpenCamera()
        {
            var cameraManager = GetCameraManager();

            string cameraId = cameraManager.GetCameraIdList()[0];

            CameraCharacteristics cameraCharacteristics
                = cameraManager.GetCameraCharacteristics(cameraId);

            var configurationMap = (StreamConfigurationMap)cameraCharacteristics.Get
            (
                CameraCharacteristics.ScalerStreamConfigurationMap
            );

            _imageDimension = configurationMap.GetOutputSizes(Class.FromType(typeof(SurfaceTexture)))[0];

            // Add permission for camera and let user grant the permission
            /*if (ActivityCompat.checkSelfPermission(this, Manifest.permission.CAMERA) != PackageManager.PERMISSION_GRANTED && ActivityCompat.checkSelfPermission(this, Manifest.permission.WRITE_EXTERNAL_STORAGE) != PackageManager.PERMISSION_GRANTED) {
             ActivityCompat.requestPermissions(AndroidCameraApi.this, new String[]{Manifest.permission.CAMERA, Manifest.permission.WRITE_EXTERNAL_STORAGE
                    }, REQUEST_CAMERA_PERMISSION);
                return;
            }*/

            cameraManager.OpenCamera(cameraId,
                                     new CameraDeviceStateCallback(CreateCameraDevice,
                                                                   CreateCameraPreview),
                                     null);
        }

        private CameraManager GetCameraManager()
        {
            var cameraManager = (CameraManager)GetSystemService(CameraService);

            return cameraManager;
        }

        private void CreateCameraDevice
        (
            CameraDevice cameraDevice
        )
        {
            _cameraDevice = cameraDevice;
        }

        private void CreateCameraPreview()
        {
            SurfaceTexture surfaceTexture = TextureView.SurfaceTexture;
            surfaceTexture.SetDefaultBufferSize(_imageDimension.Width,
                                                _imageDimension.Height);

            var surface = new Surface(surfaceTexture);
            CaptureRequest.Builder requestBuilder = _cameraDevice.CreateCaptureRequest(CameraTemplate.Preview);
            requestBuilder.AddTarget(surface);

            _cameraDevice.CreateCaptureSession(new[] { surface },
                                               new CameraPreviewStateCallback(_cameraDevice,
                                                                              requestBuilder,
                                                                              _backgroundHandler),
                                               null);
        }

        private void OrientationChanged
        (
            int deviceOrientation
        )
        {
            DeviceOrientation = deviceOrientation;
        }

        #endregion Capture Picture

        #region DeviceOrientationEventListener

        // http://android-er.blogspot.co.uk/2010/08/orientationeventlistener-detect.html

        public class DeviceOrientationEventListener : OrientationEventListener
        {
            private readonly Action<int> _orientationChanged;

            public DeviceOrientationEventListener
            (
               Context eventContext,
               SensorDelay sensorDelay,
               Action<int> orientationChanged
            )
            : base(eventContext,
                   sensorDelay)
            {
                _orientationChanged = orientationChanged;

                for (int retryNumber = 0; retryNumber < 3; retryNumber++)
                {
                    while (CanDetectOrientation() == false)
                    {
                        Thread.Sleep(200);
                    }
                }
            }

            public override void OnOrientationChanged
            (
                int deviceOrientation
            )
            {
                _orientationChanged((((deviceOrientation + 45) % 360) / 90) * 90);
            }
        }

        #endregion DeviceOrientationEventListener

        #region CameraDeviceStateCallback

        private class CameraDeviceStateCallback : CameraDevice.StateCallback
        {
            private readonly Action<CameraDevice> _openCameraDevice;
            private readonly Action _createCameraPreview;

            public CameraDeviceStateCallback
            (
                Action<CameraDevice> openCameraDevice,
                Action createCameraPreview
            )
            {
                _openCameraDevice = openCameraDevice;
                _createCameraPreview = createCameraPreview;
            }

            public override void OnOpened
            (
                CameraDevice cameraDevice
            )
            {
                _openCameraDevice(cameraDevice);
                _createCameraPreview();
            }

            /*public override void OnClosed
            (
                CameraDevice cameraDevice
            )
            {
                // TODO
            }*/

            public override void OnDisconnected
            (
                CameraDevice cameraDevice
            )
            {
                cameraDevice.Close();
            }

            public override void OnError
            (
                CameraDevice cameraDevice,
                CameraError cameraError
            )
            {
                cameraDevice.Close();
                cameraDevice = null;
            }
        }

        #endregion CameraDeviceStateCallback

        #region CameraPreviewStateCallback

        private class CameraPreviewStateCallback : CameraCaptureSession.StateCallback
        {
            private readonly CameraDevice _cameraDevice;
            private readonly CaptureRequest.Builder _requestBuilder;
            private readonly Handler _backgroundHandler;

            public CameraPreviewStateCallback
            (
                CameraDevice cameraDevice,
                CaptureRequest.Builder requestBuilder,
                Handler backgroundHandler

            )
            {
                _cameraDevice = cameraDevice;
                _requestBuilder = requestBuilder;
                _backgroundHandler = backgroundHandler;
            }

            public override void OnConfigured
            (
                CameraCaptureSession captureSession
            )
            {
                if (_cameraDevice == null)
                {
                    return; // camera is already closed
                }

                // when the session is ready, we start displaying the preview:
                UpdateCameraPreview(captureSession);
            }

            public override void OnConfigureFailed
            (
                CameraCaptureSession captureSession
            )
            {                
                throw new InvalidOperationException(nameof(OnConfigureFailed));
            }

            private void UpdateCameraPreview
            (
                CameraCaptureSession captureSession
            )
            {
                if (_cameraDevice == null)
                {
                    throw new InvalidOperationException();
                }

                // https://developer.android.com/reference/android/hardware/camera2/CameraMetadata.html#CONTROL_MODE_AUTO

                _requestBuilder.Set(CaptureRequest.ControlMode,
                                    1); // CONTROL_MODE_AUTO

                captureSession.SetRepeatingRequest(_requestBuilder.Build(),
                                                   null,
                                                   _backgroundHandler);
            }
        }

        #endregion CameraPreviewStateCallback

        #region CameraCaptureSessionCallback

        private class CameraCaptureSessionCallback : CameraCaptureSession.CaptureCallback
        {
            private readonly Action _createCameraPreview;
            private readonly Action _notifyCaptureCompleted;

            public CameraCaptureSessionCallback
            (
                Action createCameraPreview,
                Action notifyCaptureCompleted
            )
            {
                _createCameraPreview = createCameraPreview;
                _notifyCaptureCompleted = notifyCaptureCompleted;
            }

            public override void OnCaptureCompleted
            (
                CameraCaptureSession captureSession,
                CaptureRequest captureRequest,
                TotalCaptureResult captureResult
            )
            {
                base.OnCaptureCompleted(captureSession,
                                        captureRequest,
                                        captureResult);
                
                /*object jpegThumbnailSize = captureResult.Get
                (
                    CaptureResult.JpegThumbnailSize
                );*/

                /*object jpegThumbnailSize1 = captureRequest.Get
                (
                    CaptureRequest.JpegThumbnailSize
                );

                object jpegThumbnailQuality1 = captureRequest.Get
                (
                    CaptureRequest.JpegThumbnailQuality
                );

                object jpegQuality1 = captureRequest.Get
                (
                    CaptureRequest.JpegQuality
                );

                object jpegThumbnailSize2 = captureResult.Get
                (
                    CaptureResult.JpegThumbnailSize
                );

                object jpegThumbnailQuality2 = captureResult.Get
                (
                    CaptureResult.JpegThumbnailQuality
                );

                object jpegQuality2 = captureResult.Get
                (
                    CaptureResult.JpegQuality
                );*/

                _createCameraPreview();

                _notifyCaptureCompleted();
            }
        }

        #endregion CameraCaptureSessionCallback

        #region CameraCaptureStateCallback

        private class CameraCaptureStateCallback : CameraCaptureSession.StateCallback
        {
            private readonly CaptureRequest.Builder _captureBuilder;
            private readonly CameraCaptureSessionCallback _captureListener;
            private readonly Handler _backgroundHandler;

            public CameraCaptureStateCallback
            (
                CaptureRequest.Builder captureBuilder,
                CameraCaptureSessionCallback captureListener,
                Handler backgroundHandler
            )
            {
                _captureBuilder = captureBuilder;
                _captureListener = captureListener;
                _backgroundHandler = backgroundHandler;
            }

            public override void OnConfigured
            (
                CameraCaptureSession captureSession
            )
            {
                captureSession.Capture(_captureBuilder.Build(),
                                       _captureListener,
                                       _backgroundHandler);
            }

            public override void OnConfigureFailed
            (
                CameraCaptureSession captureSession
            )
            {
                // nop ?
                //throw new InvalidOperationException(nameof(OnConfigureFailed));
            }
        }

        #endregion CameraCaptureStateCallback

        #region TextureView.ISurfaceTextureListener

        void TextureView.ISurfaceTextureListener.OnSurfaceTextureAvailable
        (
            SurfaceTexture surface,
            int width,
            int height
        )
        {
            OpenCamera();
        }

        bool TextureView.ISurfaceTextureListener.OnSurfaceTextureDestroyed
        (
            SurfaceTexture surface
        )
        {
            return false;
        }

        void TextureView.ISurfaceTextureListener.OnSurfaceTextureSizeChanged
        (
            SurfaceTexture surface,
            int width,
            int height
        )
        {
            // Transform you image captured size according to the surface width and height
        }

        void TextureView.ISurfaceTextureListener.OnSurfaceTextureUpdated
        (
            SurfaceTexture surface
        )
        {
            OnReady();
        }

        #endregion TextureView.ISurfaceTextureListener

        #region ImageAvailableListener

        private class ImageAvailableListener : Object, ImageReader.IOnImageAvailableListener
        {
            private readonly File _imageFile;

            public ImageAvailableListener
            (
                File imageFile
            )
            {
                _imageFile = imageFile;
            }

            void ImageReader.IOnImageAvailableListener.OnImageAvailable
            (
                ImageReader imageReader
            )
            {
                Image latestImage = null;

                try
                {
                    OutputStream outputStream = null;
                    latestImage = imageReader.AcquireLatestImage();
                    Image.Plane[] imagePlanes = latestImage.GetPlanes();
                    ByteBuffer byteBuffer = imagePlanes[0].Buffer;
                    byte[] imageBytes = new byte[byteBuffer.Capacity()];
                    byteBuffer.Get(imageBytes);

                    try
                    {
                        outputStream = new FileOutputStream(_imageFile);
                        outputStream.Write(imageBytes);
                    }
                    finally
                    {                        
                        outputStream?.Close();                      
                    }
                }
                finally
                {                    
                    latestImage?.Close();                   
                }
            }
        }

        #endregion ImageAvailableListener
    }
}

