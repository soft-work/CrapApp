using System;

using Android.Content.Res;
using Android.Views;
using Android.Widget;

using Soft.Crap.Exceptions;
using Soft.Crap.Objects;

namespace Soft.Crap.Android.Adapters
{
    public abstract class AndroidBrowseThumbnailsAdapter<T> : BaseAdapter<T>
    {        
        private readonly Func<ImageView.ScaleType> _getScaleType;
        private readonly Action<ImageView.ScaleType> _setScaleType;       

        private int _thumbnailWidth;

        protected readonly Action<Action> RunOnUiThread;
        protected readonly Func<ViewGroup, View> InflateView;
        protected readonly Func<Resources> GetResources;
        protected readonly Action<PortableBaseObject> EditObject;
        protected readonly Action<CorruptObjectException, ImageView> OnCorrupt;        

        public AndroidBrowseThumbnailsAdapter
        (
            Action<Action> runOnUiThread,
            Func<ViewGroup, View> inflateView,
            Func<ImageView.ScaleType> getScaleType,
            Action<ImageView.ScaleType> setScaleType,
            Func<Resources> getResources,
            Action<PortableBaseObject> editObject,
            Action<CorruptObjectException, ImageView> onCorrupt
        )
        {
            _getScaleType = getScaleType;
            _setScaleType = setScaleType;

            GetResources = getResources;
            RunOnUiThread = runOnUiThread;
            InflateView = inflateView;            
            EditObject = editObject;
            OnCorrupt = onCorrupt;
        }

        public int GetThumbnailWidth()
        {
            return _thumbnailWidth;
        }

        protected void TrySetThumbnailWidth
        (
            int thumbnailWidth
        )
        {
            if ((_thumbnailWidth == 0) && (thumbnailWidth > 0))
            {
                _thumbnailWidth = thumbnailWidth;
            }
        }

        protected void HandleThumbnailScaleType
        (
            ImageView thumbnailView            
        )
        {
            ImageView.ScaleType thumbnailScaleType = thumbnailView.GetScaleType();
            ImageView.ScaleType activityScaleType = _getScaleType();

            if (activityScaleType == null)
            {
                _setScaleType(thumbnailScaleType);
            }
            else if (activityScaleType != thumbnailScaleType)
            {
                thumbnailView.SetScaleType(activityScaleType);
            }
        }
    }
}