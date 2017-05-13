using System;

using Android.App;
using Android.Content;
using Android.Widget;
using Android.OS;

using Soft.Crap.Android.Adapters;
using Soft.Crap.Android.Fragments;

namespace Soft.Crap.Android.Activities
{    
    public abstract class AndroidBrowseThumbnailsActivity<T> : AndroidBrowseThumbnailsActivity
    {
        private bool _abandonViewState = false;

        protected abstract int BrowseViewId { get; }
        protected abstract int BrowseLayoutResource { get; }
        protected abstract int ItemLayoutResource { get; }

        protected abstract AndroidBrowseThumbnailsAdapter<T> GetBrowseAdapter();
        protected abstract Type ToggleBrowseType();

        protected abstract IParcelable BrowseViewState { set; get; }
        protected abstract int FirstVisiblePosition { set; get; }
        protected abstract int FirstVisibleRow { set; get; }
        protected abstract int? PositionsInRow { set; get; }

        protected virtual int? LoadingResource
        {
            get { return null; }
        }

        protected virtual int ColumnCount
        {
            get { return 1; }
        }

        protected AbsListView AbstractBrowseView
        {
            private set; get;
        }

        protected GridView GridBrowseView
        {
            get { return AbstractBrowseView as GridView; }
        }

        protected AndroidBrowseThumbnailsAdapter<T> BrowseViewAdapter { private set; get; }                
        protected AndroidBrowseFooterFragment BrowseFooterFragment { private set; get; }

        protected byte CorruptMesssageCount = 5;
        protected bool IsHandlingException { set; get; } 

        protected override void OnCreate
        (
            Bundle bundle
        )
        {
            base.OnCreate(bundle);

            this.SetStatusBarColor();
            this.HideActionBarIcon();
            SetTitle(Resource.String.ApplicationName);

            SetContentView(BrowseLayoutResource);            
        }

        protected override void OnResume()
        {
            base.OnResume();

            if (IsHandlingException)
            {
                return;
            }

            BrowseFooterFragment = FragmentManager.FindFragmentById<AndroidBrowseFooterFragment>
            (
                Resource.Id.BrowseFooterFragment
            );

            AbstractBrowseView = FindViewById<AbsListView>(BrowseViewId);

            BrowseViewAdapter = GetBrowseAdapter();

            BrowseFooterFragment.ConfigureBrowsingFor
            (
                AbstractBrowseView,

                getThumbnailWidth: () =>
                {
                    return BrowseViewAdapter.GetThumbnailWidth();
                },

                viewTypeChanged: () =>
                {
                    ToggleBrowseActivity();
                },

                scaleTypeChanged: () =>
                {
                    BrowseViewAdapter.NotifyDataSetChanged();
                }
            );

            if (AbstractBrowseView.Adapter == null)
            {
                AbstractBrowseView.Adapter = BrowseViewAdapter;
            }

            if (BrowseViewState != null)
            {
                AbstractBrowseView.OnRestoreInstanceState(BrowseViewState);
            }
            else
            {
                TryRestoreFirstPosition();
            }

            //_browseObjectsList.ChoiceMode = ChoiceMode.Multiple;
            //AbsListView.IMultiChoiceModeListener multiChoiceModeListener = new MultiChoiceModeListener();
            //_browseObjectsList.SetMultiChoiceModeListener(multiChoiceModeListener);            
        }

        protected override void OnPause()
        {
            base.OnPause();

            if (IsHandlingException)
            {
                return;
            }

            // https://futurestud.io/tutorials/how-to-save-and-restore-the-scroll-position-and-state-of-a-android-listview

            if (BrowseViewState != null)
            {
                BrowseViewState.Dispose();
                BrowseViewState = null;
            }

            if (_abandonViewState == false)
            {
                BrowseViewState = AbstractBrowseView.OnSaveInstanceState();
            }

            if (ColumnCount == 1)
            {
                FirstVisiblePosition = AbstractBrowseView.FirstVisiblePosition;
            }

            FirstVisibleRow = AbstractBrowseView.FirstVisiblePosition / ColumnCount;
            PositionsInRow = ColumnCount;            
        }

        protected override void OnDestroy()
        {
            BrowseViewState?.Dispose();
            BrowseViewAdapter?.Dispose();            

            base.OnDestroy();            
        }

        private void TryRestoreFirstPosition()
        {
            if (PositionsInRow == null)
            {
                return;
            }

            int firstPositionInRow = FirstVisibleRow * PositionsInRow.Value;
            int lastPositionInRow = firstPositionInRow + PositionsInRow.Value - 1;

            int firstVisiblePosition = ((FirstVisiblePosition < firstPositionInRow) ||
                                        (lastPositionInRow < FirstVisiblePosition))
                                     ? firstPositionInRow
                                     : FirstVisiblePosition;

            if (AbstractBrowseView.FirstVisiblePosition != firstVisiblePosition)
            {
                AbstractBrowseView.SetSelection(firstVisiblePosition);
            }
        }
        
        protected void ToggleBrowseActivity()
        {
            Type browseType = ToggleBrowseType();

            _abandonViewState = true;

            StartBrowseActivity(this,
                                browseType,
                                clearTop : true);
        }
    }

    public abstract class AndroidBrowseThumbnailsActivity : Activity
    {
        public static void StartBrowseActivity
        (
            Context callerContext,
            Type browseType
        )
        {
            StartBrowseActivity(callerContext,
                                browseType,
                                clearTop : false);
        }

        protected static void StartBrowseActivity
        (
            Context callerContext,
            Type browseType,
            bool clearTop
        )
        {
            var browseIntent = new Intent(callerContext,
                                          browseType);
            if (clearTop)
            {
                browseIntent.SetFlags(ActivityFlags.ClearTop);
            }

            callerContext.StartActivity(browseIntent);
        }
    };
}

