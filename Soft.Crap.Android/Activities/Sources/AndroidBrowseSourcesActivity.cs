using System;
using System.Collections.Generic;
using System.Linq;

using Android.App;
using Android.Content;
using Android.Views;

using Soft.Crap.Android.Activities.Objects;
using Soft.Crap.Android.Adapters;
using Soft.Crap.Sources;

using SourceItem = System.Collections.Generic.KeyValuePair<Soft.Crap.Sources.PortableBaseSource,
                                                           int>;

namespace Soft.Crap.Android.Activities.Sources
{
    public abstract class AndroidBrowseSourcesActivity
                        : AndroidBrowseThumbnailsActivity<SourceItem>
    {
        private const int SlideInterval = 3000;                
        
        private static AndroidBrowseSourcesAdapter _sourceListAdapter;

        // static to be shared between list and grid 'view styles':
        private static int _firstVisiblePosition { set; get; }
        private static int _firstVisibleRow { set; get; }
        private static int? _positionsInRow { set; get; }        

        protected override AndroidBrowseThumbnailsAdapter<SourceItem> GetBrowseAdapter()
        {
            return _sourceListAdapter;
        }

        protected override Type ToggleBrowseType()
        {            
            Type browseType = (this is AndroidSourceListActivity)
                            ? typeof(AndroidSourceGridActivity)
                            : typeof(AndroidSourceListActivity);

            return browseType;
        }        

        protected override int FirstVisiblePosition
        {
            set { _firstVisiblePosition = value; }

            get { return _firstVisiblePosition; }
        }

        protected override int FirstVisibleRow
        {
            set { _firstVisibleRow = value; }

            get { return _firstVisibleRow; }
        }

        protected override int? PositionsInRow
        {
            set { _positionsInRow = value; }

            get { return _positionsInRow; }
        }

        // http://martynnw.blogspot.co.uk/2014/10/xamarin-android-listviews-checkboxes-on.html

        /*private void SourceItemLongClicked
        (
            object sender,
            AdapterView.ItemLongClickEventArgs arguments
        )
        {
            EditObject(_browseSourcesAdapter[arguments.Position].Key.SourceObjects[0]);
        }*/

        protected override void OnResume()
        {
            bool initializeAdapter = ((BrowseViewState == null) ||                                      
                                      (PortableObjectRepository<Activity>.HasUnreadSourceChanges));
            if (initializeAdapter)
            {
                List<PortableBaseSource> objectSources
                    = PortableObjectRepository<Activity>.GetObjectSources().OrderBy
                (
                    objectSource => (!objectSource.IsEnabled) +
                                    objectSource.ProviderName +
                                    objectSource.SourceName +
                                    objectSource.SourceDetails
                )
                .ToList();

                _sourceListAdapter = new AndroidBrowseSourcesAdapter
                (                    
                    RunOnUiThread,

                    (viewGroup) => LayoutInflater.Inflate(ItemLayoutResource,
                                                          viewGroup,
                                                          attachToRoot : false),

                    () => { return BrowseFooterFragment.GetScaleType(); },
                    (scaleType) => BrowseFooterFragment.SetScaleType(scaleType),

                    () => { return Resources; },

                    this.EditObject,

                    objectSources,

                    onCorrupt : (imageException,
                                 imageView) =>
                    {
                        this.HandleCorruptObject
                        (
                            imageException,
                            imageView,
                            getCount : () => { return CorruptMesssageCount; },
                            setCount : (currentMassageCount) => { CorruptMesssageCount = currentMassageCount; }
                        );
                    }
                );                
            }

            base.OnResume();            

            _sourceListAdapter.StartTimer(SlideInterval);
        }

        protected override void OnPause()
        {
            _sourceListAdapter.StopTimer();

            base.OnPause();
        }

        public override bool OnCreateOptionsMenu
        (
            IMenu optionsMenu
        )
        {
            MenuInflater.Inflate(Resource.Menu.SourceViewOptionsMenu,
                                 optionsMenu);

            return base.OnCreateOptionsMenu(optionsMenu);
        }

        public override bool OnOptionsItemSelected
        (
            IMenuItem menuItem
        )
        {
            switch(menuItem.ItemId)
            {
                case Resource.Id.EditSourceItem:
                {
                    //EditSourceClicked();

                    var objectGridIntent = new Intent(this,
                                                      typeof(AndroidObjectGridActivity));
                        
                    StartActivity(objectGridIntent);

                    break;
                }

                case Resource.Id.MoreOptionsItem:
                {
                    BrowseFooterFragment.ShowPopupMenu(Resource.Menu.BrowseViewPopupMenu);

                    break;
                }

                default:
                {
                    throw new InvalidOperationException(menuItem.TitleFormatted.ToString());
                }
            }

            return base.OnOptionsItemSelected(menuItem);
        }

        public async override void OnBackPressed()
        {
            _sourceListAdapter.StopTimer();

            if (_sourceListAdapter.IsDataChanged)
            {
                IEnumerable<PortableBaseSource> objectSources
                    = PortableObjectRepository<Activity>.GetObjectSources();

                await PortableSourceRepository.SaveSourceDataAsync
                (
                    objectSources,
                    AndroidCrapApplication.ApplicationLogger
                );

                await PortableObjectRepository<Activity>.RefreshObjectCacheAsync
                (
                    AndroidCrapApplication.ApplicationLogger,
                    
                    updateCount : (objectCount) => { }
                );
            }

            base.OnBackPressed();
        }        
    }
}

