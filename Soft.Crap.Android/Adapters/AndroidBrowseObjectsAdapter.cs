using System;
using System.Collections.Generic;

using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.Views;
using Android.Widget;

using Soft.Crap.Android.Caching;
using Soft.Crap.Android.Correlation;
using Soft.Crap.Android.Rendering;
using Soft.Crap.Correlation;
using Soft.Crap.Exceptions;
using Soft.Crap.Objects;
using Soft.Crap.Rendering;

namespace Soft.Crap.Android.Adapters
{
    public class AndroidBrowseObjectsAdapter 
               : AndroidBrowseThumbnailsAdapter<PortableSyncRenderer<Context>>
    {        
        private readonly int? _loadingResource;                
        private readonly List<PortableSyncRenderer<Context>> _objectRenderers;
        private readonly Func<int, string> _getString;        

        public AndroidBrowseObjectsAdapter
        (
            Action<Action> runOnUiThread,
            Func<ViewGroup, View> inflateView,
            Func<ImageView.ScaleType> getScaleType,
            Action<ImageView.ScaleType> setScaleType,
            Func<Resources> getResources,
            int? loadingResource,
            Action<PortableBaseObject> editObject,          
            List<PortableSyncRenderer<Context>> objectRenderers,
            Func<int, string> getString,
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
            _loadingResource = loadingResource;            
            _objectRenderers = objectRenderers;
            _getString = getString;            
        }                

        public override int Count
        {
            get { return _objectRenderers.Count; }
        }

        public override PortableSyncRenderer<Context> this
        [
            int itemPosition
        ]
        {
            get { return _objectRenderers[itemPosition]; }
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
            View objectView,
            ViewGroup viewGroup
        )
        {
            bool isRecycled = (objectView != null);

            if (isRecycled == false)
            {
                objectView = InflateView(viewGroup);
            }

            TextView typeDescription = objectView.FindViewById<TextView>(Resource.Id.TypeDescription);
            TextView objectTime = objectView.FindViewById<TextView>(Resource.Id.ObjectTime);
            TextView objectLocation = objectView.FindViewById<TextView>(Resource.Id.ObjectLocation);
            AndroidCachingImageView objectThumbnail = objectView.FindViewById<AndroidCachingImageView>(Resource.Id.ObjectThumbnail);
            ImageView typeThumbnail = objectView.FindViewById<ImageView>(Resource.Id.TypeThumbnail);
            TextView sourceDescription = objectView.FindViewById<TextView>(Resource.Id.SourceDescription);            
            TextView objectDescription = objectView.FindViewById<TextView>(Resource.Id.ObjectDescription);
            TextView objectDetails = objectView.FindViewById<TextView>(Resource.Id.ObjectDetails);

            HandleThumbnailScaleType(objectThumbnail);
            
            PortableSyncRenderer<Context> syncRenderer = this[itemPosition];

            if (syncRenderer.ObjectDrawable.HasValue)
            {
                objectThumbnail.SetImageDrawable(GetResources(),
                                                 syncRenderer.ObjectDrawable.Value);
            }
            else
            {
                objectThumbnail.SetImageDrawable(null);
            }

            if (objectTime != null)
            {
                objectTime.Text = string.Format("{0}",
                                                syncRenderer.ObjectTime);
            }

            if (sourceDescription != null)
            {
                sourceDescription.Text = syncRenderer.SourceDescription;
            }

            if (typeThumbnail != null)
            {
                typeThumbnail.SetImageResource(syncRenderer.TypeDrawable);
            }

            if (typeDescription != null)
            {
                typeDescription.Text = string.Format("{0} ({1})",
                                                     _getString(syncRenderer.TypeName),
                                                     syncRenderer.TypeDescription);
            }

            string initialDescription = null;

            if (objectDescription != null)
            {
                initialDescription = syncRenderer.ObjectDescription;
                objectDescription.Text = initialDescription;                
            }

            // it is save to remove event handler even if not added, yet:
            objectThumbnail.Click -= OnObjectThumbnailClicked;
            objectThumbnail.Click += OnObjectThumbnailClicked;

            var asyncRenderer = syncRenderer as PortableAsyncRenderer<Bitmap>;
            var correlatedEntity = syncRenderer as PortableCorrelatedEntity;
            
            AndroidCrapApplication.ApplicationLogger.LogDebug("{0} - {1} : {2}",
                                                              itemPosition,
                                                              correlatedEntity.CorrelationTag,
                                                              GC.GetTotalMemory(false));

            if ((asyncRenderer != null) && (correlatedEntity != null))
            {
                if (objectDescription != null)
                {
                    objectDescription.Tag = correlatedEntity.CorrelationTag;

                    asyncRenderer.FireParallelTextRendering
                    (
                        correlatedEntity,                        
                        RunOnUiThread,
                        initialDescription,
                        objectDescription
                    );
                }

                objectThumbnail.Tag = correlatedEntity.CorrelationTag;

                if (isRecycled == false)
                {
                    objectView.Tag = objectThumbnail.Background;
                }                                    

                asyncRenderer.FireParallelThumbnailRendering
                (
                    correlatedEntity,                    
                    RunOnUiThread,
                    objectView,
                    objectThumbnail,
                    GetResources(),
                    _loadingResource,
                    OnCorrupt,
                    onFinished : (thumbnailView,
                                  originalBackground,
                                  loadingAnimation,
                                  isLoaded) =>
                    {
                        loadingAnimation?.Stop();

                        if (thumbnailView.Background != originalBackground)
                        {
                            thumbnailView.Background = originalBackground;
                        }

                        TrySetThumbnailWidth(thumbnailView.Width);                        
                    }
                );
            }

            return objectView;
        }

        public void AddPendingObject
        (
            PortableBaseObject pendingObject
        )
        {
            var objectRenderer = (PortableSyncRenderer<Context>)pendingObject;
            
            _objectRenderers.Add(objectRenderer);
        }

        private void OnObjectThumbnailClicked
        (
            object sender,
            EventArgs arguments
        )
        {
            PortableBaseObject selectedObject = null;

            try
            {
                ImageView objectThumbnail = (ImageView)sender;

                string correlationTag = objectThumbnail.GetCorrelationTag();

                selectedObject = PortableObjectRepository<Activity>.GetObjectByCorrelationTag
                (
                    correlationTag
                );                                
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