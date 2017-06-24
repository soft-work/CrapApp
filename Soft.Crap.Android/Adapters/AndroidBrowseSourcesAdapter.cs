using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Android.Content.Res;
using Android.Graphics;
using Android.Views;
using Android.Widget;

using Soft.Crap.Android.Caching;
using Soft.Crap.Android.Rendering;
using Soft.Crap.Correlation;
using Soft.Crap.Exceptions;
using Soft.Crap.Objects;
using Soft.Crap.Rendering;
using Soft.Crap.Sources;

namespace Soft.Crap.Android.Adapters
{
    public class AndroidBrowseSourcesAdapter 
               : AndroidBrowseThumbnailsAdapter<KeyValuePair<PortableBaseSource, int>>                                               
    {        
        private const int initialIndex = -1;
                
        private readonly List<KeyValuePair<PortableBaseSource, int>> _objectSources;

        private Timer _slideTimer;        

        public AndroidBrowseSourcesAdapter                
        (
            Action<Action> runOnUiThread,
            Func<ViewGroup, View> inflateView,
            Func<ImageView.ScaleType> getScaleType,
            Action<ImageView.ScaleType> setScaleType,
            Func<Resources> getResources,
            Action<PortableBaseObject> editObject,
            List<PortableBaseSource> objectSources,
            Action<CorruptObjectException, ImageView> onCorrupt
        )
        : base
        (
            runOnUiThread,
            inflateView,
            getScaleType,
            setScaleType,
            getResources,
            editObject,
            onCorrupt
        )
        {
            _objectSources = objectSources.Select
            (
                objectSource => new KeyValuePair<PortableBaseSource, int>(objectSource,
                                                                          initialIndex)
            )
            .ToList();            

            IsDataChanged = false;
        }        

        protected override void Dispose
        (
            bool disposing
        )
        {
            if (disposing)
            {
                StopTimer();
            }

            base.Dispose(disposing);
        }

        public void StartTimer
        (
            int slideInterval
        )
        {
            if (_slideTimer != null)
            {
                throw new InvalidOperationException(nameof(_slideTimer));
            }

            _slideTimer = new Timer
            (
                state =>
                {                                        
                    RunOnUiThread(NotifyDataSetChanged);                    
                }
            );

            _slideTimer.Change(0, slideInterval);
        }

        public void StopTimer()
        {
            if (_slideTimer == null)
            {
                return;
            }

            _slideTimer.Dispose();
            _slideTimer = null;
        }

        public bool IsDataChanged { private set; get; }        

        public override int Count
        {
            get { return _objectSources.Count; }
        }

        public override KeyValuePair<PortableBaseSource, int> this
        [
            int itemPosition
        ]
        {
            get { return _objectSources[itemPosition]; }
        }

        public override long GetItemId
        (
            int itemPosition
        )
        {
            return itemPosition;
        }

        public override View GetView
        (
            int itemPosition,
            View sourceView,
            ViewGroup viewGroup
        )
        {
            sourceView = sourceView ?? InflateView(viewGroup);

            ImageView sourceThumbnail = sourceView.FindViewById<ImageView>(Resource.Id.SourceThumbnail);
            CheckBox sourceEnabled = sourceView.FindViewById<CheckBox>(Resource.Id.SourceEnabled);
            //TextView sourceDescription = sourceView.FindViewById<TextView>(Resource.Id.SourceDescription);
            TextView sourceDetails = sourceView.FindViewById<TextView>(Resource.Id.SourceDetails);
            AndroidCachingImageView objectThumbnail = sourceView.FindViewById<AndroidCachingImageView>(Resource.Id.ObjectThumbnail);

            HandleThumbnailScaleType(objectThumbnail);

            KeyValuePair<PortableBaseSource, int> objectSource = this[itemPosition];
            sourceView.Tag = itemPosition;

            if (sourceThumbnail != null)
            {
                if (objectSource.Key.ProviderName == "Phone") // TODO hardcoded!
                {
                    sourceThumbnail.SetImageResource(Resource.Drawable.perm_group_display);
                }
                else
                {
                    sourceThumbnail.SetImageResource(Resource.Drawable.perm_group_user_dictionary);
                }

                if (sourceEnabled != null)
                {
                    sourceThumbnail.Tag = sourceEnabled;

                    // it is save to remove event handler even if not added, yet:
                    sourceThumbnail.Click -= OnSourceThumbnailClicked;
                    sourceThumbnail.Click += OnSourceThumbnailClicked;
                }
            }

            if (sourceEnabled != null)
            {
                // http://stackoverflow.com/questions/15403417/unable-to-align-checkboxhorizontally-vertically-in-android-app

                string sourceDescription = objectSource.Key.ProviderName;

                if (objectSource.Key.SourceName != null)
                {
                    sourceDescription += " " + objectSource.Key.SourceName;
                }

                sourceEnabled.Checked = objectSource.Key.IsEnabled;
                sourceEnabled.Tag = new JavaObjectWrapper<PortableBaseSource>(objectSource.Key);
                sourceEnabled.Clickable = true;
                sourceEnabled.CheckedChange -= OnSourceEnabledChange;
                sourceEnabled.CheckedChange += OnSourceEnabledChange;

                sourceEnabled.Text = string.Format("{0} ({1})",
                                                   sourceDescription,
                                                   objectSource.Key.SourceObjects.Count);
            }

            if (sourceDetails != null)
            {
                sourceDetails.Text = objectSource.Key.SourceDetails;
            }

            var correlatedEntity = objectSource.Key as PortableCorrelatedEntity;

            // it is save to remove event handler even if not added, yet:
            objectThumbnail.Click -= OnSlideThumbnailClicked;
            objectThumbnail.Click += OnSlideThumbnailClicked;

            if (correlatedEntity != null)
            {
                objectThumbnail.Tag = correlatedEntity.CorrelationTag;

                FireParallelSlideRendering(itemPosition,
                                           sourceView,
                                           objectThumbnail);
            }

            return sourceView;
        }

        private void FireParallelSlideRendering
        (
            int itemPosition,
            View sourceView,
            AndroidCachingImageView objectThumbnail
        )
        {            
            if (objectThumbnail.Visibility != ViewStates.Visible)
            {
                return;
            }

            int objectIndex = _objectSources[itemPosition].Value;
            int objectCount = _objectSources[itemPosition].Key.SourceObjects.Count;

            if (objectIndex == objectCount - 1)
            {
                objectIndex = initialIndex;
            }

            objectIndex++;

            PortableAsyncRenderer<Bitmap> asyncRenderer = null;

            PortableBaseSource objectSource = _objectSources[itemPosition].Key;

            if (objectIndex < objectSource.SourceObjects.Count)
            {
                asyncRenderer = objectSource.SourceObjects[objectIndex] as PortableAsyncRenderer<Bitmap>;
            }

            if (asyncRenderer == null)
            {
                objectThumbnail.SetImageDrawable(null);

                return;
            }

            if (objectThumbnail.Visibility != ViewStates.Visible)
            {
                return;
            }
            
            int? loadingResource = null;

            asyncRenderer.FireParallelThumbnailRendering
            (
                _objectSources[itemPosition].Key,                
                RunOnUiThread,
                sourceView,
                objectThumbnail,
                GetResources(),
                loadingResource,
                OnCorrupt,
                onFinished : (thumbnailView,
                              originalBackground,
                              loadingAnimation,
                              isLoaded) =>
                {
                    TrySetThumbnailWidth(thumbnailView.Width);

                    if (isLoaded == false)
                    {
                        return;
                    }                    

                    _objectSources[itemPosition] = new KeyValuePair<PortableBaseSource, int>
                    (
                        _objectSources[itemPosition].Key,
                        objectIndex
                    );                    
                }
            );
        }

        private void OnSourceThumbnailClicked
        (
            object sender,
            EventArgs arguments
        )
        {
            ImageView sourceThumbnail = (ImageView)sender;

            CheckBox sourceEnabled = (CheckBox)sourceThumbnail.Tag;

            sourceEnabled.Checked = !sourceEnabled.Checked;
        }

        private void OnSourceEnabledChange
        (
            object sender,
            EventArgs arguments
        )
        {
            CheckBox sourceEnabled = (CheckBox)sender;            

            var checkboxTag = (JavaObjectWrapper<PortableBaseSource>)sourceEnabled.Tag;

            PortableBaseSource objectSource = checkboxTag.WrappedObject;

            objectSource.IsEnabled = sourceEnabled.Checked;

            IsDataChanged = true;
        }

        private void OnSlideThumbnailClicked
        (
            object sender,
            EventArgs arguments
        )
        {            
            PortableBaseObject selectedObject = null;

            try
            {
                ImageView slideThumbnail = (ImageView)sender;

                FrameLayout backgroundFrame = slideThumbnail.GetParentView<FrameLayout>();
                FrameLayout innerFrame = backgroundFrame.GetParentView<FrameLayout>();
                FrameLayout outerFrame = innerFrame.GetParentView<FrameLayout>();
                ViewGroup sourceView = outerFrame.GetParentView<ViewGroup>();                

                int itemPosition = (int)sourceView.Tag;

                KeyValuePair<PortableBaseSource, int> selectedSource = this[itemPosition];

                PortableBaseSource objectSource = selectedSource.Key;
                int objectIndex = selectedSource.Value;

                selectedObject = objectSource.SourceObjects[objectIndex];
            }
            catch(Exception exception)
            {
                AndroidCrapApplication.ApplicationLogger.LogError(exception);

                throw;
            }
 
            if (selectedObject != null)
            {
                EditObject(selectedObject);
            }
        }
    }
}