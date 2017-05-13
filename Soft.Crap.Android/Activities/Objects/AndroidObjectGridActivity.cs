using Android.App;
using Android.OS;

namespace Soft.Crap.Android.Activities.Objects
{
    [Activity(Theme = "@style/Soft.Crap")]

    public class AndroidObjectGridActivity : AndroidBrowseObjectsActivity
    {
        private IParcelable _browseViewState;

        protected override IParcelable BrowseViewState
        {
            set { _browseViewState = value; }
            get { return _browseViewState; }
        }

        protected override int BrowseViewId
        {
            get { return Resource.Id.ObjectGridView; }
        }

        protected override int BrowseLayoutResource
        {
            get { return Resource.Layout.ObjectGridLayout; }
        }

        protected override int ItemLayoutResource
        {
            get { return Resource.Layout.ObjectCellLayout; }
        }

        protected override int? LoadingResource
        {
            get { return Resource.Drawable.ProgressCircle; }
        }

        protected override int ColumnCount
        {
            get { return GridBrowseView.NumColumns; }
        }        
    }
}

