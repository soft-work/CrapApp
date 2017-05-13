using System;
using System.Threading.Tasks;

using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.Hardware;
using Android.OS;
using Android.Provider;
using Android.Util;
using Android.Views;
using Android.Widget;

using Soft.Crap.Android.Gestures;
using Soft.Crap.Exceptions;

namespace Soft.Crap.Android.Activities.Images
{
    [Activity(Theme = "@style/Soft.Crap.Camera")]

    public class AndroidEditImageActivity : Activity
    {
        public const string DeviceOrientationExtra = "DeviceOrientation";
        public const string PicturePathExtra = "PicturePath";

        private string _picturePath;

        private ImageView _imageView;
        private Button _attributesButton;
        private Button _rotateButton;

        private OrientationEventListener _orientationListener;
        private int? _deviceOrientation;

        private ScaleGestureDetector _scaleDetector;
        private float _scaleFactor = 1.0f;

        private GestureDetector _scrollDetector;

        protected override void OnCreate
        (
            Bundle bundle
        )
        {
            base.OnCreate(bundle);

            _picturePath = Intent.GetStringExtra(PicturePathExtra);

            if (_picturePath == null)
            {
                throw new InvalidOperationException(string.Format("'{0}' extra string missing.",
                                                    PicturePathExtra));
            }

            _orientationListener = new AndroidCameraActivity.DeviceOrientationEventListener
            (
                this,
                SensorDelay.Normal,
                OrientationChanged
            );

            _orientationListener.Enable();

            SetContentView(Resource.Layout.EditImageLayout);

            _imageView = FindViewById<ImageView>(Resource.Id.EditedImage);
            _imageView.Touch -= TouchImage;
            _imageView.Touch += TouchImage;

            _attributesButton = FindViewById<Button>(Resource.Id.AttributesButton);
            _attributesButton.Visibility = ViewStates.Invisible;
            _attributesButton.Click -= EditAttributes;
            _attributesButton.Click += EditAttributes;

            _attributesButton.LongClick -= DragButton;
            _attributesButton.LongClick += DragButton;

            _rotateButton = FindViewById<Button>(Resource.Id.RotateButton);
            _rotateButton.Visibility = ViewStates.Invisible;
            _rotateButton.Click -= RotateImage;
            _rotateButton.Click += RotateImage;

            _rotateButton.LongClick -= DragButton;
            _rotateButton.LongClick += DragButton;

            var scaleListener = new AndroidScaleListener
            (
                getScaleFactor: () => _scaleFactor,

                setScaleFactor: (scaleFactor) =>
                {
                    _scaleFactor = scaleFactor;
                    _imageView.ScaleX = _scaleFactor;
                    _imageView.ScaleY = _scaleFactor;
                },

                minScaleFactor : 1,

                maxScaleFactor : 10
            );

            _scaleDetector = new ScaleGestureDetector(this,
                                                      scaleListener);

            var flingListener = new AndroidFlingListener
            (
                getViewSize : () => new Size(_imageView.Width,
                                             _imageView.Height),

                getScaleFactor : () => _scaleFactor,

                getScrollPoint : () => new PointF(_imageView.ScrollX,
                                                  _imageView.ScrollY),
                minHorizontalDistance : 100,
                minVerticalDistance : 100,

                minHorizontalVelocity : 200,
                minVerticalVelocity : 200,

                moveScrolledView : (horizontalDistance,
                                    verticalDistance) => _imageView.ScrollBy
                (
                    (int)Math.Round(horizontalDistance),
                    (int)Math.Round(verticalDistance)
                )                
            );

            _scrollDetector = new GestureDetector(flingListener);            

            // http://stackoverflow.com/questions/3591784/getwidth-and-getheight-of-view-returns-0
            _imageView.Post // wait with image loading until view fully initialised
            (
                () =>

                Task.Run
                (
                    () =>

                    LoadImageAsync()
                )
            );
        }                

        protected override void OnResume()
        {
            base.OnResume();

            _orientationListener.Enable();
        }

        protected override void OnPause()
        {
            _orientationListener.Disable();            

            base.OnPause();
        }

        protected override void OnDestroy()
        {
            _imageView.SetImageDrawable(null);
            _orientationListener.Disable();

            base.OnDestroy();            
        }

        private void OrientationChanged
        (
            int deviceOrientation
        )
        {
            _deviceOrientation = deviceOrientation;
        }

        private void TouchImage
        (
            object sender,
            View.TouchEventArgs arguments
        )
        {
            bool isHandled = _scrollDetector.OnTouchEvent(arguments.Event);            

            if (isHandled == false)
            {
                isHandled = _scaleDetector.OnTouchEvent(arguments.Event);
            }

            arguments.Handled = isHandled;
        }

        private void DragButton
        (
            object sender,
            View.LongClickEventArgs arguments
        )
        {
            View draggedButton = (View)sender;

            draggedButton.DragOver(_imageView);

            arguments.Handled = true;
        }

        private async void LoadImageAsync()
        {
            if (_deviceOrientation == null)
            {
                _deviceOrientation = Intent.GetIntExtra(DeviceOrientationExtra,
                                                        OrientationEventListener.OrientationUnknown);
            }

            if (_deviceOrientation == OrientationEventListener.OrientationUnknown)
            {
                _deviceOrientation = 0;
            }

            // http://stackoverflow.com/questions/20902775/how-to-check-if-auto-rotate-screen-setting-is-on-off-in-android-4-0
            bool autoRotation = (Settings.System.GetInt(ContentResolver,
                                                        Settings.System.AccelerometerRotation, 0) == 1);

            int pictureOrientation = await AndroidExifHandler.GetPictureOrientationAsync(_picturePath);
            if (autoRotation == false)
            {
                pictureOrientation += (360 - _deviceOrientation.Value) % 360;
            }

            int viewWidth = _imageView.Width;
            int viewHeight = _imageView.Height;

            DisplayMetrics displayMetrics = Resources.DisplayMetrics;
            double displayWidth = displayMetrics.WidthPixels;
            var displayHeight = displayMetrics.HeightPixels;

            Size bitmapSize = await AndroidBitmapHandler.GetBitmapSizeAsync(_picturePath);

            int imageWidth = bitmapSize.Width;
            int imageHeight = bitmapSize.Height;

            // when auto-rotation switched off, image view rectangle may be 'wrongly rotated' for
            // the image width / height as saved (without taking picture rotation flag into account):
            if ((autoRotation == false) ||
                ((displayWidth - displayHeight) * (imageWidth - imageHeight) < 0))
            {
                viewWidth = _imageView.Height;
                viewHeight = _imageView.Width;
            }

            Bitmap imageBitmap = null;
            CorruptObjectException imageException = null;

            try
            {
                // resize the bitmap to fit the display - loading the full sized image will consume too much memory:
                imageBitmap = await AndroidBitmapHandler.LoadAdjustedBitmapAsync
                (
                    _picturePath,
                    viewWidth,
                    viewHeight,
                    pictureOrientation
                );
            }
            catch(Exception exception)
            {
                imageException = exception as CorruptObjectException;
                                
                if (imageException == null)
                {
                    throw;
                } 
            }            

            RunOnUiThread
            (
                () =>

                {
                    if (imageException != null)
                    {
                        this.HandleCorruptObject(imageException,
                                                 _imageView);
                    }
                    else
                    {
                        using(imageBitmap)
                        {
                            _imageView.SetImageBitmap(imageBitmap);
                        }

                        imageBitmap = null;

                        _attributesButton.Visibility = ViewStates.Visible;
                        _rotateButton.Visibility = ViewStates.Visible;
                    }                    
                }
            );
        }

        private void EditAttributes
        (
            object sender,
            EventArgs arguments
        )
        {
            
        }

        private async void RotateImage
        (
            object sender,
            EventArgs arguments
        )
        {
            int pictureOrientation = await AndroidExifHandler.GetPictureOrientationAsync(_picturePath);

            pictureOrientation = (pictureOrientation + 90) % 360;

            await AndroidExifHandler.SetPictureOrientationAsync(_picturePath,
                                                                pictureOrientation);
            LoadImageAsync();
        }
    }
}

