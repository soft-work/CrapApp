using Android.App;
using Android.OS;

namespace Soft.Crap.Android.Activities.Sources
{
    [Activity(Theme = "@style/Soft.Crap")]

    public class AndroidSourceListActivity : AndroidBrowseSourcesActivity
    {
        private IParcelable _browseViewState;

        protected override IParcelable BrowseViewState
        {
            set { _browseViewState = value; }
            get { return _browseViewState; }
        }

        protected override int BrowseViewId
        {
            get { return Resource.Id.SourceListView; }
        }

        protected override int BrowseLayoutResource
        {
            get { return Resource.Layout.SourceListLayout; }
        }

        protected override int ItemLayoutResource
        {
            get { return Resource.Layout.SourceRowLayout; }
        }                
    }
}

