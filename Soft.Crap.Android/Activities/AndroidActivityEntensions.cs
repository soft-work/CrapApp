using System;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Views;
using Android.Widget;

using Soft.Crap.Exceptions;
using Soft.Crap.Objects;
using Soft.Crap.Rendering;

namespace Soft.Crap.Android.Activities
{
    // https://stackoverflow.com/jobs/135892/senior-or-mid-level-xamarin-engineer-just-eat

    // http://stackoverflow.com/questions/22192291/how-to-change-the-status-bar-color-in-android

    public static class AndroidActivityEntensions
    {
        public static void SetStatusBarColor
        (
            this Activity thisActivity
        )
        {
            Window activityWindow = thisActivity.Window;

            // clear FLAG_TRANSLUCENT_STATUS flag:
            activityWindow.ClearFlags(WindowManagerFlags.TranslucentStatus);

            // add FLAG_DRAWS_SYSTEM_BAR_BACKGROUNDS flag to the window
            activityWindow.AddFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);

            // finally change the color
            activityWindow.SetStatusBarColor(Color.Navy);
        }

        public static void HideActionBarIcon
        (
            this Activity thisActivity
        )
        {
            ActionBar actionBar = thisActivity.ActionBar;

            if (actionBar == null)
            {
                throw new InvalidOperationException(nameof(HideActionBarIcon));
            }

            actionBar.SetDisplayOptions(0, ActionBarDisplayOptions.HomeAsUp);
            actionBar.SetDisplayOptions(0, ActionBarDisplayOptions.ShowCustom);
            actionBar.SetDisplayOptions(0, ActionBarDisplayOptions.ShowHome);
            actionBar.SetDisplayOptions(0, ActionBarDisplayOptions.UseLogo);
        }

        public static void HandleCorruptObject
        (
            this Activity thisActivity,
            CorruptObjectException imageException,
            ImageView imageView,
            Func<byte> getCount = null,
            Action<byte> setCount = null
        )
        {
            if ((getCount != null) && (setCount == null))
            {
                throw new ArgumentNullException(nameof(setCount));
            }
            else if ((getCount == null) && (setCount != null))
            {
                throw new ArgumentNullException(nameof(getCount));
            }
            else if ((getCount != null) && (setCount != null))
            {
                byte currentCount = getCount();

                if (currentCount == 0)
                {
                    return;
                }

                setCount(--currentCount);
            }                        

            imageView.SetImageResource(Resource.Drawable.CorruptObject);

            string exceptionFormat = thisActivity.GetString(Resource.String.CorruptObjectFormat);
            string exceptionMessage = string.Format(exceptionFormat,
                                                    imageException.Message);
            Toast.MakeText
            (
                thisActivity,
                exceptionMessage,
                ToastLength.Long
            )
            .Show();
        }        

        public static void EditObject
        (
            this Activity thisActivity,
            PortableBaseObject editedObject
        )
        {
            var syncRenderer = (PortableSyncRenderer<Context>)editedObject;

            syncRenderer.EditObject(thisActivity,
                                    deviceOrientation : 0);
        }

        // <!-- http://stackoverflow.com/questions/3263611/border-for-an-image-view-in-android -->

        // https://forums.xamarin.com/discussion/295/get-real-screen-size

        // https://developer.android.com/training/displaying-bitmaps/manage-memory.html ?!

        // https://developer.xamarin.com/api/type/Android.Views.Display/

        // iOS: https://forums.xamarin.com/discussion/19778/uiimage-rotation-and-transformation
        // https://developer.xamarin.com/recipes/ios/media/images/rotate_an_image/

        // http://navyuginfo.com/efficient-image-loading-in-android/

        // https://components.xamarin.com/view/square.picasso

        // iOS: https://gist.github.com/aliozgur/179a98e6c7a548758c3c

        // Xamarin https://forums.xamarin.com/discussion/33078/get-the-gist-of-it-contest         

        // https://developer.xamarin.com/recipes/android/resources/device_specific/detect_screen_size/

        // http://stackoverflow.com/questions/8981845/android-rotate-image-in-imageview-by-an-angle

        // http://stackoverflow.com/questions/7729133/using-asynctask-to-load-images-in-listview

        // 97:6E:52:CE:E1:91:B4:87:0C:5F:1C:E8:ED:D7:E4:8F:85:4A:6D:B9

        // https://www.vectorstock.com/royalty-free-vectors/icons-vectors?bsa=1

        // https://stories.uplabs.com/animated-icons-on-android-ee635307bd6#.ao0nrv3ai

        // http://www.peko-step.com/en/tool/alphachannel_en.html transparent transparency

        // https://www.youtube.com/watch?v=ghQAp1KUd6k
    }
}

