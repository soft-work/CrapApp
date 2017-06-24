using System;
using System.Threading.Tasks;

using Android.App;
using Android.Content;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Widget;

using Soft.Crap.Android.Activities.Objects;

using Environment = System.Environment;

namespace Soft.Crap.Android.Activities.Splash
{
    [Activity(MainLauncher = true, NoHistory = true, Theme = "@style/Soft.Crap.Splash")]

    public class AndroidSplashScreenActivity : Activity
    {
        private static int _progressChunk = Environment.ProcessorCount * 5;
        private static Task _loadingTask;
        private static TextView _progressCount;
        private static Context _loadingContext;

        protected override async void OnCreate
        (
            Bundle bundle
        )
        {
            //AndroidCrapApplication.ApplicationLogger.LogDebug("MEMORY 4 : {0}",
            //                                                  GC.GetTotalMemory(false));

            base.OnCreate(bundle);

            SetContentView(Resource.Layout.SplashScreenLayout);

            _progressCount = FindViewById<TextView>(Resource.Id.ProgressCount);      

            ImageView splashProgress = FindViewById<ImageView>(Resource.Id.SplashProgress);
            splashProgress.SetBackgroundResource(Resource.Drawable.ProgressSphere);

            AnimationDrawable splashAnimation = splashProgress.Background as AnimationDrawable;
            Exception loadingException = null;

            //AndroidCrapApplication.ApplicationLogger.LogDebug("MEMORY 5 : {0}",
            //                                                  GC.GetTotalMemory(false));

            splashAnimation?.Start();
            _loadingContext = this;

            if (_loadingTask == null)
            {
                _loadingTask = CreateLoadingTask
                (
                    GetString(Resource.String.SplashTextFormat),

                    updateText : (progressText) => UpdateText(progressText),

                    getContext : () => { return GetContext(); }
                );
            }

            //AndroidCrapApplication.ApplicationLogger.LogDebug("MEMORY 6 : {0}",
            //                                                  GC.GetTotalMemory(false));

            try
            {                
                await _loadingTask;
            }
            catch(Exception exception)
            {
                loadingException = exception;
            }
            finally
            {
                _loadingTask = null;

                splashAnimation?.Stop();
                splashAnimation?.Dispose();
                splashAnimation = null;
                splashProgress = null;
            }

            /*// http://stackoverflow.com/questions/477572/strange-out-of-memory-issue-while-loading-an-image-to-a-bitmap-object

            Java.Lang.JavaSystem.RunFinalization();
            Java.Lang.JavaSystem.Gc();
            Java.Lang.JavaSystem.RunFinalization();
            System.Runtime.GCSettings.LargeObjectHeapCompactionMode
                = System.Runtime.GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect();*/

            if (loadingException != null)
            {
                AndroidCrapApplication.ShowExceptionAndExit(this,
                                                            loadingException);
                return;
            }

            //AndroidCrapApplication.ApplicationLogger.LogDebug("MEMORY 7 : {0}",
            //                                                  GC.GetTotalMemory(false));
        }

        protected override void OnDestroy()
        {            
            base.OnDestroy();

            _loadingContext = null;            
        }

        private static Task CreateLoadingTask
        (
            string progressFormat,
            Action<string> updateText,
            Func<Context> getContext
        )
        {
            return Task.Run
            (
                async () =>
                {
                    await PortableObjectRepository<Activity>.RefreshObjectCacheAsync
                    (
                        AndroidCrapApplication.ApplicationLogger,

                        updateCount: (objectCount) =>
                        {
                            if (objectCount % _progressChunk != 0)
                            {
                                return;
                            }

                            string progressText = string.Format(progressFormat,
                                                                objectCount);
                            updateText(progressText);
                        }
                    );                    

                    Context loadingContext = getContext();

                    AndroidBrowseThumbnailsActivity.StartBrowseActivity(loadingContext,
                                                                        typeof(AndroidObjectListActivity));
                }
            );
        }

        private static void UpdateText
        (
            string progressText
        )
        {
            _progressCount.Post
            (
                () => { _progressCount.Text = progressText; }
            );
        }

        private static Context GetContext()
        {
            return _loadingContext;
        }
    }
}

