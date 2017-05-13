using Android.OS;
using Android.App;
using Android.Views;

namespace Soft.Crap.Android.Fragments
{
    public class AndroidEditImageFragment : Fragment
    {        
        public override View OnCreateView
        (
            LayoutInflater inflater,
            ViewGroup container,
            Bundle bundle
        )
        {
            View fragment = inflater.Inflate(Resource.Layout.EditImageFragment,
                                             container,
                                             attachToRoot : false);
            return fragment;
        }
    }
}