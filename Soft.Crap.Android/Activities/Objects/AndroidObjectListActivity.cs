using Android.App;
using Android.OS;

namespace Soft.Crap.Android.Activities.Objects
{
    [Activity(Theme = "@style/Soft.Crap")]

    public class AndroidObjectListActivity : AndroidBrowseObjectsActivity
    {
        private IParcelable _browseViewState;

        protected override IParcelable BrowseViewState
        {
            set { _browseViewState = value; }
            get { return _browseViewState; }
        }

        protected override int BrowseViewId
        {
            get { return Resource.Id.ObjectListView; }
        }

        protected override int BrowseLayoutResource
        {
            get { return Resource.Layout.ObjectListLayout; }
        }

        protected override int ItemLayoutResource
        {
            get { return Resource.Layout.ObjectRowLayout; }
        }        
    }
}

