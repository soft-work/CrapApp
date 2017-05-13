using System;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Widget;

using Soft.Crap.Android.Objects;
using Soft.Crap.Objects;

namespace Soft.Crap.Android.Activities.Images
{
    [Activity(Theme = "@style/Soft.Crap.Camera",
              ScreenOrientation = ScreenOrientation.Portrait)]

    public class AndroidAddImageActivity : AndroidCameraActivity
    {
        public const string PicturePathExtra = "PicturePath";

        private string _picturePath;
        private Button _captureButton;

        protected override void OnCreate
        (
            Bundle bundle
        )
        {
            base.OnCreate(bundle);

            SetContentView(Resource.Layout.AddImageLayout);

            TextureView = FindViewById<TextureView>(Resource.Id.TextureView);

            _captureButton = FindViewById<Button>(Resource.Id.CaptureButton);
            _captureButton.Visibility = ViewStates.Invisible;
            _captureButton.Click -= CapturePicture;
            _captureButton.Click += CapturePicture;
        }

        #region CapturePictureActivity

        protected override void OnReady()
        {
            _captureButton.Visibility = ViewStates.Visible;
        }

        protected override void OnDone()
        {
            PortableFileObject imageObject = PortableObjectRepository<Activity>.AddFileObject
            (
                _picturePath,

                (directorySource, objectFile) => new AndroidImageObject(directorySource,
                                                                        objectFile)
            );

            PortableObjectRepository<Activity>.PushPendingObject(imageObject);

            if (DeviceOrientation == null)
            {
                // this should "never" happen:
                throw new NullReferenceException(nameof(DeviceOrientation));
            }

            var editImageIntent = new Intent(this, typeof(AndroidEditImageActivity));

            editImageIntent.PutExtra(AndroidEditImageActivity.DeviceOrientationExtra,
                                     DeviceOrientation.Value);

            editImageIntent.PutExtra(AndroidEditImageActivity.PicturePathExtra,
                                     _picturePath);

            StartActivity(editImageIntent);
        }

        private void CapturePicture
        (
            object sender,
            EventArgs arguments
        )
        {
            _picturePath = Intent.GetStringExtra(PicturePathExtra);

            if (_picturePath == null)
            {
                throw new InvalidOperationException(string.Format("'{0}' extra string missing.",
                                                    PicturePathExtra));
            }

            CapturePicture(_picturePath);
        }

        #endregion CapturePictureActivity
    }
}

