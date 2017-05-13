using System;

using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Runtime;
using Android.Views;

using Soft.Crap.Android.Caching;
using Soft.Crap.Android.Logging;
using Soft.Crap.Exceptions;
using Soft.Crap.IO;
using Soft.Crap.Logging;

using Environment = System.Environment;
using Object = Java.Lang.Object;
using Path = System.IO.Path;
using Runtime = Java.Lang.Runtime;

namespace Soft.Crap.Android
{

#if DEBUG

    [Application(Icon = "@drawable/CrapAppIcon", Debuggable = true)]

#else

    [Application(Icon = "@drawable/CrapAppIcon", Debuggable = false)]

#endif

    public class AndroidCrapApplication : Application
    {
        private const string SourceFileName = "ObjectSources.xml";        

        public static PortableContextLogger ApplicationLogger { private set; get; }        

        public static Activity CurrentActivity { private set; get; }

        private static AndroidBitmapDrawableCache _bitmapCache;

        public AndroidCrapApplication
        (
            IntPtr javaReference,
            JniHandleOwnership handleOwnership
        )
        : base
        (
            javaReference,
            handleOwnership
        )
        {            
            ApplicationLogger = new AndroidContextLogger();

            UnobservedExceptionHandler.RegisterPlatformSpecific
            (
                exceptionLogger : ApplicationLogger,
                exceptionClear : ExceptionClear,
                exceptionNotification : (errorMessage) => { }
            );            
        }

        public override void OnLowMemory()
        {
            base.OnLowMemory();            
        }

        public override void OnTrimMemory
        (
            [GeneratedEnum] TrimMemory level
        )
        {
            base.OnTrimMemory(level);          
        }                          

        public override void OnCreate()
        {
            ApplicationLogger.LogDebug("MEMORY 0 : {0}",                                                                          
                                       GC.GetTotalMemory(false));
            base.OnCreate();

            ApplicationLogger.LogDebug("MEMORY 1 : {0}",
                                       GC.GetTotalMemory(false));

            UnhandledExceptionHandler.Activate();

            CreateBitmapCache();

            RegisterActivityLifecycleCallbacks(new ActivityLifecycleCallbacks());

            PortableObjectRepository<Activity>.RegisterPlatformSpecific
            (
                getUiContext: () => { return CurrentActivity; },

                showExceptionAndExit : (activity,
                                        exception) => ShowExceptionAndExit(activity,
                                                                           exception),
                fileObjectDescriber : new DefaultFileDescriber(), 

                objectLoadingTask : AndroidObjectRepository.LoadAndroidObjectsAsync               
            );

            PortableSourceRepository.RegisterPlatformSpecific
            (                
                sourceReaderFactory : AndroidSourceRepository.CreateSourceReader,

                sourceWriterFactory : AndroidSourceRepository.CreateSourceWriter
            );

            ApplicationLogger.LogDebug("MEMORY 2 : {0}",
                                       GC.GetTotalMemory(false));
        }

        public static string GetSourceFilePath()
        {
            string filePath = Path.GetFullPath
            (
                Path.Combine
                (
                    Environment.GetFolderPath(Environment.SpecialFolder.Personal),

                    SourceFileName
                )
            );

            return filePath;
        }        

        public static void ShowExceptionAndExit
        (
            Activity activity,
            Exception exception
        )
        {
            ExceptionClear();

            Context context = /*activity ??*/ Context;

            var dialogBuilder = new AlertDialog.Builder(context)
                                               .SetTitle(Resource.String.ApplicationName)
                                               .SetMessage(exception.GetAggregatedMessage())
                                               .SetPositiveButton
            (
                Resource.String.AlertCloseButton,

                delegate
                {
                    Environment.Exit(-1);
                }
            );

            //activity.RunOnUiThread
            //(            
                //() =>
                //{
                    Dialog exceptionDialog = dialogBuilder.Create();

                    //if (activity == null)
                    {
                        exceptionDialog.Window.SetType(WindowManagerTypes.SystemAlert);
                    }

                    exceptionDialog.Show();
                //}
            //);
        }

        private static void ExceptionClear()
        {
            // http://www.developer.com/java/data/exception-handling-in-jni.html

            if (JNIEnv.ExceptionOccurred() != IntPtr.Zero)
            {
                JNIEnv.ExceptionClear();
            }
        }

        private void CreateBitmapCache()
        {
            // https://github.com/rdio/tangoandcache

            long highWatermark = Runtime.GetRuntime().MaxMemory() / 3;
            long lowWatermark = highWatermark / 2;

            // The GC threshold is the amount of bytes that have been evicted from the cache
            // that will trigger a GC.Collect. For example if set to 2mb, a GC will be performed
            // each time the cache has evicted a total of 2mb.
            // This means that we can have highWatermark + gcThreshold amount of memory in use
            // before a GC is forced, so we should ensure that the threshold value + hightWatermark
            // is less than our total memory.
            // In our case, the highWatermark is 1/3 of max memory, so using the same value for the
            // GC threshold means we can have up to 2/3 of max memory in use before kicking the GC.

            long gcThreshold = highWatermark;

            _bitmapCache = new AndroidBitmapDrawableCache(highWatermark,
                                                          lowWatermark,
                                                          gcThreshold);
        }

        public static BitmapDrawable GetReusableBitmapDrawable
        (
            int bitmapWidth,
            int bitmapHeight
        )
        {
            AndroidCachingBitmapDrawable bitmapDrawable = _bitmapCache.GetReusableBitmapDrawable
            (
                bitmapWidth,
                bitmapHeight
            );

            return bitmapDrawable;
        }

        public static AndroidCachingBitmapDrawable AddBitmapToCache
        (            
            string cacheKey,
            Bitmap cachedBitmap,
            Resources resources
        )
        {            
            var bitmapDrawable = new AndroidCachingBitmapDrawable(resources,
                                                                  cachedBitmap);
            _bitmapCache.AddBitmapDrawableToCache
            (
                cacheKey,
                bitmapDrawable
            );

            return bitmapDrawable;
        }
        
        public static BitmapDrawable GetDrawableFromCache
        (
            string cacheKey
        )
        {
            BitmapDrawable bitmapDrawable = _bitmapCache.GetBitmapDrawableFromCache(cacheKey);

            return bitmapDrawable;
        }

        private class ActivityLifecycleCallbacks : Object, IActivityLifecycleCallbacks
        {
            // http://stackoverflow.com/questions/3873659/android-how-can-i-get-the-current-foreground-activity-from-a-service/38650587#38650587

            void IActivityLifecycleCallbacks.OnActivityCreated
            (
                Activity activity,
                Bundle bundle
            )
            {
                CurrentActivity = activity;
            }

            void IActivityLifecycleCallbacks.OnActivityStarted
            (
                Activity activity
            )
            {
                CurrentActivity = activity;
            }

            void IActivityLifecycleCallbacks.OnActivityResumed
            (
                Activity activity
            )            
            {
                CurrentActivity = activity;
            }

            void IActivityLifecycleCallbacks.OnActivityPaused
            (
                Activity activity
            )
            {
                CurrentActivity = null;
            }

            void IActivityLifecycleCallbacks.OnActivityStopped
            (
                Activity activity
            )
            {
                // don't clear current activity because activity may get stopped after
                // the new activity is resumed
            }

            void IActivityLifecycleCallbacks.OnActivitySaveInstanceState
            (
                Activity activity,
                Bundle bundle
            )
            {
                // nothing to do here - no activity change
            }

            void IActivityLifecycleCallbacks.OnActivityDestroyed
            (
                Activity activity
            )
            {
                // don't clear current activity because activity may get destroyed after
                // the new activity is resumed
            }
        }        
    }
}