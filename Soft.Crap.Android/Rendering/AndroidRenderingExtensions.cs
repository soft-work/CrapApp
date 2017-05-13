using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Android.Content.Res;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Views;
using Android.Widget;

using Soft.Crap.Android.Caching;
using Soft.Crap.Android.Correlation;
using Soft.Crap.Correlation;
using Soft.Crap.Exceptions;
using Soft.Crap.Rendering;

using Exception = System.Exception;

namespace Soft.Crap.Android.Rendering
{
    public static class AndroidRenderingExtensions
    {
        public static void FireParallelTextRendering
        (
            this PortableAsyncRenderer<Bitmap> asyncRenderer,
            PortableCorrelatedEntity correlatedEntity,            
            Action<Action> runOnUiThread,
            string initialText,
            TextView textView
        )
        {
            Task.Run
            (
                () =>

                asyncRenderer.RenderTextAsync(correlatedEntity,                                              
                                              runOnUiThread,
                                              initialText,
                                              textView)
            );
        }
        
        public static void FireParallelThumbnailRendering
        (
            this PortableAsyncRenderer<Bitmap> asyncRenderer,
            PortableCorrelatedEntity correlatedEntity,            
            Action<Action> runOnUiThread,
            View itemView,
            AndroidCachingImageView thumbnailView,
            Resources activityResources,
            int? backgroundResource,
            Action<CorruptObjectException, ImageView> onCorrupt,
            Action<ImageView, Drawable, AnimationDrawable, bool> onFinished
        )
        {
            Drawable originalBackground = itemView.Tag as Drawable;
            AnimationDrawable loadingAnimation = thumbnailView.Background as AnimationDrawable;

            bool isLoaded = false;

            if (correlatedEntity.CorrelationTag != thumbnailView.GetCorrelationTag())
            {
                onFinished?.Invoke(thumbnailView,
                                   originalBackground,
                                   loadingAnimation,
                                   isLoaded);
                return;
            }            

            if (backgroundResource != null)
            {                
                thumbnailView.SetBackgroundResource(backgroundResource.Value);
            }
            
            loadingAnimation?.Start();         

            itemView.Post // to have image view measured size reliable
            (
                async () =>

                {
                    int thumbnailWidth = thumbnailView.MeasuredWidth;
                    int thumbnailHeight = thumbnailView.MeasuredHeight;

                    if (thumbnailView.Visibility != ViewStates.Visible)
                    {                        
                        onFinished?.Invoke(thumbnailView,
                                           originalBackground,
                                           loadingAnimation,
                                           isLoaded);
                        return;
                    }

                    try
                    {                        
                        await asyncRenderer.RenderThumbnailAsync(correlatedEntity,
                                                                 runOnUiThread,
                                                                 thumbnailView,
                                                                 thumbnailWidth,
                                                                 thumbnailHeight,
                                                                 activityResources,
                                                                 originalBackground,
                                                                 loadingAnimation,
                                                                 onCorrupt,
                                                                 onFinished);
                    }
                    catch(Exception exception)
                    {
                        AndroidCrapApplication.ApplicationLogger.LogError(exception);

                        throw;
                    }
                }            
            );
        }

        private static async void RenderTextAsync
        (
            this PortableAsyncRenderer<Bitmap> asyncRenderer,
            PortableCorrelatedEntity correlatedEntity,            
            Action<Action> runOnUiThread,
            string initialText,
            TextView textView            
        )
        {
            if (correlatedEntity.CorrelationTag != textView.GetCorrelationTag())
            {                
                return;
            }

            IReadOnlyDictionary<string, object> objectAttributes;

            try
            {
                objectAttributes = await asyncRenderer.GetAttributesAsync
                (
                    correlatedEntity,
                    () => textView.GetCorrelationTag()
                );
            }
            catch(Exception exception)
            {
                AndroidCrapApplication.ApplicationLogger.LogError(exception);
                throw;
            }

            if (correlatedEntity.CorrelationTag != textView.GetCorrelationTag())
            {
                return;
            }

            runOnUiThread
            (
                () =>

                {
                    if (correlatedEntity.CorrelationTag != textView.GetCorrelationTag())
                    {
                        return;
                    }

                    if (objectAttributes == null)
                    {
                        return;
                    }

                    textView.Text = initialText /* string.Join
                    (
                        " * ",
                        objectAttributes.Value.Values
                    )*/;
                }
            );
        }

        private static async Task RenderThumbnailAsync
        (
            this PortableAsyncRenderer<Bitmap> asyncRenderer,
            PortableCorrelatedEntity correlatedEntity,
            Action<Action> runOnUiThread,
            AndroidCachingImageView thumbnailView,
            int thumbnailWidth,
            int thumbnailHeight,
            Resources activityResources,
            Drawable originalBackground,
            AnimationDrawable loadingAnimation,
            Action<CorruptObjectException, ImageView> onCorrupt,
            Action<ImageView, Drawable, AnimationDrawable, bool> onFinished
        )
        {
            bool isLoaded = false;

            if (correlatedEntity.CorrelationTag != thumbnailView.GetCorrelationTag())
            {                
                onFinished?.Invoke(thumbnailView,
                                   originalBackground,
                                   loadingAnimation,
                                   isLoaded);
                return;
            }            

            Bitmap thumbnailBitmap = null;
            CorruptObjectException thumbnailException = null;

            BitmapDrawable thumbnailDrawable = AndroidCrapApplication.GetDrawableFromCache
            (
                correlatedEntity.CorrelationTag
            );

            if (thumbnailDrawable == null)
            {
                try
                {
                    /*thumbnailDrawable = AndroidCrapApplication.GetReusableBitmapDrawable(thumbnailWidth,
                                                                                         thumbnailHeight);*/       
                    thumbnailBitmap = await asyncRenderer.GetThumbnailAsync
                    (
                        correlatedEntity,
                        () => thumbnailView.GetCorrelationTag(),
                        thumbnailWidth,
                        thumbnailHeight//,
                        //thumbnailDrawable.Bitmap
                    );
                }
                catch(Exception exception)
                {
                    AndroidCrapApplication.ApplicationLogger.LogError(exception);
                    thumbnailBitmap?.Dispose();

                    thumbnailException = exception as CorruptObjectException;

                    if (thumbnailException == null)
                    {
                        throw;
                    }
                    else
                    {
                        runOnUiThread
                        (
                            () => onCorrupt(thumbnailException,
                                            thumbnailView)
                        );
                    }

                    onFinished?.Invoke(thumbnailView,
                                       originalBackground,
                                       loadingAnimation,
                                       isLoaded);
                    return;
                }

                if (correlatedEntity.CorrelationTag != thumbnailView.GetCorrelationTag())
                {
                    thumbnailBitmap?.Dispose();

                    onFinished?.Invoke(thumbnailView,
                                       originalBackground,
                                       loadingAnimation,
                                       isLoaded);
                    return;
                }

                if (thumbnailBitmap == null)
                {
                    onFinished?.Invoke(thumbnailView,
                                       originalBackground,
                                       loadingAnimation,
                                       isLoaded);
                    return;
                }

                thumbnailDrawable = AndroidCrapApplication.AddBitmapToCache
                (
                    correlatedEntity.CorrelationTag,
                    thumbnailBitmap,
                    activityResources
                );
            }

            isLoaded = true;

            onFinished?.Invoke(thumbnailView,
                               originalBackground,
                               loadingAnimation,
                               isLoaded);            
            runOnUiThread
            (
                () => { thumbnailView.SetImageDrawable(thumbnailDrawable); }
            );            

            AndroidCrapApplication.ApplicationLogger.LogDebug("{0} : {1}",
                                                              correlatedEntity.CorrelationTag,
                                                              GC.GetTotalMemory(false));
        }
    }
}
