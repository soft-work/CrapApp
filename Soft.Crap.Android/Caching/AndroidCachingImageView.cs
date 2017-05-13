using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Net;
using Android.Util;
using Android.Widget;

// http://brett.duncavage.org/2014/02/in-memory-bitmap-caching-with.html
// https://github.com/rdio/tangoandcache

namespace Soft.Crap.Android.Caching
{        
    public class AndroidCachingImageView : ImageView
    {
        public AndroidCachingImageView
        (
            Context context
        )
        : base(context) { }

        public AndroidCachingImageView
        (
            Context context,
            IAttributeSet attributes
        )
        : base(context,
              attributes) { }

        public AndroidCachingImageView
        (
            Context context,
            IAttributeSet attributes,
            int defStyle
        )
        : base(context,
               attributes,
               defStyle) { }

        public void SetImageDrawable
        (
            Resources contextResources,
            int imageResource
        )
        {
            Bitmap resourceBitmap = BitmapFactory.DecodeResource(contextResources, imageResource);
            Drawable bitmapDrawable = new BitmapDrawable(contextResources, resourceBitmap);

            SetImageDrawable(bitmapDrawable);
        }

        public override void SetImageDrawable
        (
            Drawable newDrawable
        )
        {
            Drawable oldDrawable = Drawable;

            base.SetImageDrawable(newDrawable);

            UpdateDrawableDisplayState(oldDrawable,
                                       false);

            UpdateDrawableDisplayState(newDrawable,
                                       true);
        }

        public override void SetImageResource
        (
            int resourceId
        )
        {
            Drawable oldDrawable = Drawable;

            base.SetImageResource(resourceId);

            UpdateDrawableDisplayState(oldDrawable,
                                       false);
        }

        public override void SetImageURI
        (
            Uri imageUri
        )
        {
            Drawable oldDrawable = Drawable;

            base.SetImageURI(imageUri);

            UpdateDrawableDisplayState(oldDrawable,
                                       false);
        }

        private void UpdateDrawableDisplayState
        (
            Drawable bitmapDrawable,
            bool isDisplayed
        )
        {
            var cachingDrawable = bitmapDrawable as AndroidCachingBitmapDrawable;

            if (cachingDrawable != null)
            {
                cachingDrawable.IsDisplayed = isDisplayed;
            }
            else 
            {
                var layerDrawable = bitmapDrawable as LayerDrawable;

                if (layerDrawable != null)
                {
                    for (int layerIndex = 0; layerIndex < layerDrawable.NumberOfLayers; layerIndex++)
                    {
                        UpdateDrawableDisplayState(layerDrawable.GetDrawable(layerIndex),
                                                   isDisplayed);
                    }
                }
            }
        }
    }
}