using System;
using System.Collections.Generic;
using System.Linq;

using Android.App;
using Android.Content;
using Android.Views;
using Android.Widget;

using Soft.Crap.Android.Adapters;
using Soft.Crap.Objects;

using Soft.Crap.Android.Activities.Images;
using Soft.Crap.Android.Activities.Sources;

using Environment = Android.OS.Environment;
using File = Java.IO.File;
using ObjectItem = Soft.Crap.Rendering.PortableSyncRenderer<Android.Content.Context>;

namespace Soft.Crap.Android.Activities.Objects
{
    public abstract class AndroidBrowseObjectsActivity 
                        : AndroidBrowseThumbnailsActivity<ObjectItem>
    {        
        private static AndroidBrowseObjectsAdapter _objectViewAdapter;

        // static to be shared between list and grid 'view styles':
        private static int _firstVisiblePosition { set; get; }
        private static int _firstVisibleRow { set; get; }
        private static int? _positionsInRow { set; get; }        

        protected override AndroidBrowseThumbnailsAdapter<ObjectItem> GetBrowseAdapter()
        {
            return _objectViewAdapter;
        }

        protected override Type ToggleBrowseType()        
        {            
            Type browseType = (this is AndroidObjectListActivity)
                            ? typeof(AndroidObjectGridActivity)
                            : typeof(AndroidObjectListActivity);

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

        protected override void OnResume()
        {
            bool initializeAdapter = ((BrowseViewState == null) ||                                      
                                      (PortableObjectRepository<Activity>.HasUnreadSourceChanges));
            if (initializeAdapter)
            {
                List<ObjectItem> objectRenderers;

                try
                {
                    objectRenderers 
                        = PortableObjectRepository<Activity>.GetEnabledObjects<ObjectItem>()
                                                            .ToList();
                }
                catch(Exception exception)
                {
                    IsHandlingException = true;

                    base.OnResume();

                    AndroidCrapApplication.ShowExceptionAndExit(this,
                                                                exception);
                    return;
                }                

                _objectViewAdapter = new AndroidBrowseObjectsAdapter
                (
                    RunOnUiThread,
                   
                    (viewGroup) =>
                    {
                        try
                        {
                            return LayoutInflater.Inflate(ItemLayoutResource,
                                                          viewGroup,
                                                          attachToRoot : false);
                        }
                        catch(Exception exception)
                        {
                            throw exception;
                        }
                    },

                    () => { return BrowseFooterFragment.GetScaleType(); },
                    (scaleType) => BrowseFooterFragment.SetScaleType(scaleType),

                    () => { return Resources; },

                    LoadingResource,

                    this.EditObject,

                    objectRenderers,

                    getString : (resource) => GetString(resource),

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

            AbstractBrowseView.ItemClick -= ObjectItemClicked;
            AbstractBrowseView.ItemClick += ObjectItemClicked;            

            bool isDataChanged = false;

            PortableBaseObject pendingObject = PortableObjectRepository<Activity>.PopPendingObject();

            while(pendingObject != null)
            {
                _objectViewAdapter.AddPendingObject(pendingObject);

                isDataChanged = true;

                pendingObject = PortableObjectRepository<Activity>.PopPendingObject();
            }

            if (isDataChanged)
            {
                _objectViewAdapter.NotifyDataSetChanged();
            }
        }

        public override bool OnCreateOptionsMenu
        (
            IMenu optionsMenu
        )
        {
            // https://developer.android.com/guide/topics/data/data-storage.html#pref

            MenuInflater.Inflate(Resource.Menu.ObjectViewOptionsMenu,
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
                case Resource.Id.AddObjectItem:
                {
                    AddImageClicked();

                    break;
                }

                case Resource.Id.SearchObjectsItem:
                {
                    SearchObjectsClicked();

                    break;
                }

                /*case Resource.Id.DeleteObjectItem:
                {
                    DeleteReceiptClicked();

                    break;
                }*/

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

        // http://stackoverflow.com/questions/25192573/how-do-i-debug-on-a-real-android-device-using-xamarin-for-visual-studio
        // https://androidmtk.com/download-samsung-usb-drivers

        // http://www.tutorialsbuzz.com/2014/07/custom-expandable-listview-image-text.html
        // http://www.tutorialsbuzz.com/2014/03/watsapp-custom-listview-imageview-textview-baseadapter.html        

        private void ObjectItemClicked
        (
            object sender,
            AdapterView.ItemClickEventArgs arguments
        )
        {
            Toast.MakeText(this, "DUPA", ToastLength.Long).Show();
        }

        /*private void ObjectItemLongClicked
        (
            object sender,
            AdapterView.ItemLongClickEventArgs arguments
        )
        {
            PortableSyncRenderer<Context> selectedObject = _browseObjectsAdapter[arguments.Position];

            selectedObject.EditObject(this,
                                      deviceOrientation : 0);
        }*/

        private void AddImageClicked()
        {
            var imageDirectory = new File
            (
                Environment.GetExternalStoragePublicDirectory(Environment.DirectoryPictures),

                PackageName
            );

            if (imageDirectory.Exists() == false)
            {
                imageDirectory.Mkdirs();
            }

            var imageFile = new File(imageDirectory,
                                     string.Format("Receipt_{0}.jpg", Guid.NewGuid()));

            var addImageIntent = new Intent(this, typeof(AndroidAddImageActivity));

            addImageIntent.PutExtra(AndroidAddImageActivity.PicturePathExtra,
                                    imageFile.Path);

            StartActivity(addImageIntent);
        }

        private void SearchObjectsClicked()
        {
            var browseSourcesIntent = new Intent(this,
                                                 typeof(AndroidSourceListActivity));            

            StartActivity(browseSourcesIntent);
        }

        /*private class MultiChoiceModeListener : Object, AbsListView.IMultiChoiceModeListener
        {            
            public bool OnActionItemClicked
            (
                ActionMode mode,
                IMenuItem item
            )
            {
                throw new NotImplementedException();
            }

            public bool OnCreateActionMode
            (
                ActionMode mode,
                IMenu menu
            )
            {
                throw new NotImplementedException();
            }

            public void OnDestroyActionMode
            (
                ActionMode mode
            )
            {
                throw new NotImplementedException();
            }

            public void OnItemCheckedStateChanged
            (
                ActionMode mode,
                int position,
                long id,
                bool @checked
            )
            {
                throw new NotImplementedException();
            }

            public bool OnPrepareActionMode
            (
                ActionMode mode,
                IMenu menu
            )
            {
                throw new NotImplementedException();
            }            
        }*/

        /* private void AddReceiptClicked()
        {
            if (IsThereAnAppToTakePictures() == false)
            {
                new AlertDialog.Builder(this)
                               .SetTitle(Resource.String.DeleteReceiptTitle)
                               .SetMessage(Resource.String.NoCameraApplicationInfo)
                               .SetPositiveButton(Resource.String.AlertCloseButton, delegate { })
                               .Show();
                return;
            }

            var receiptsDirectory = new File(Environment.GetExternalStoragePublicDirectory(Environment.DirectoryPictures),
                                             PackageName);

            if (receiptsDirectory.Exists() == false)
            {
                receiptsDirectory.Mkdirs();
            }

            TakeReceiptPicture(receiptsDirectory);            
        }*/

        /*private bool IsThereAnAppToTakePictures()
        {
            var intent = new Intent(MediaStore.ActionImageCapture);

            IList<ResolveInfo> activities = PackageManager.QueryIntentActivities(intent,
                                                                                 PackageInfoFlags.MatchDefaultOnly);
            return (activities != null) && (activities.Count > 0);
        }

        private void TakeReceiptPicture
        (
            File receiptsDirectory
        )
        {
            _receiptPicture = new File(receiptsDirectory, String.Format("Receipt_{0}.jpg", Guid.NewGuid()));

            var intent = new Intent(MediaStore.ActionImageCapture);
            //var intent = new Intent(MediaStore.IntentActionStillImageCamera);

            intent.PutExtra(MediaStore.ExtraOutput, Uri.FromFile(_receiptPicture));
            intent.PutExtra(MediaStore.ExtraScreenOrientation, true);
            intent.PutExtra(MediaStore.ExtraFullScreen, true);
            intent.PutExtra(MediaStore.ExtraShowActionIcons, false);
            intent.PutExtra(MediaStore.IntentActionStillImageCamera, true);
            intent.PutExtra(MediaStore.ExtraFinishOnCompletion, true);
            

            StartActivityForResult(intent, 0);
        }

        protected override void OnActivityResult
        (
            int requestCode,
            Result resultCode,
            Intent data
        )
        {
            base.OnActivityResult(requestCode,
                                  resultCode,
                                  data);

            var intent = new Intent(this, typeof(AddReceiptActivity));
            intent.PutExtra("PicturePath", _receiptPicture.Path);

            _receiptPicture.Dispose();
            _receiptPicture = null;

            StartActivity(intent);

            // Make it available in the gallery

            //Intent mediaScanIntent = new Intent(Intent.ActionMediaScannerScanFile);
            //Uri contentUri = Uri.FromFile(App._file);
            //mediaScanIntent.SetData(contentUri);
            //SendBroadcast(mediaScanIntent);
        }*/

        private void DeleteReceiptClicked()
        {
            new AlertDialog.Builder(this)
                           .SetTitle(Resource.String.DeleteReceiptTitle)
                           .SetMessage(Resource.String.DeleteReceiptQuestion)
                           .SetPositiveButton(Resource.String.AlertDeleteButton, AlertDeleteClicked)
                           .SetNegativeButton(Resource.String.AlertCancelButton, delegate { })
                           .Show();
        }

        private void AlertDeleteClicked
        (
            object sender,
            EventArgs arguments
        )
        {
            new AlertDialog.Builder(this)
                           .SetTitle(Resource.String.DeleteReceiptTitle)
                           .SetMessage("Done.")
                           .SetPositiveButton(Resource.String.AlertCloseButton, delegate { })
                           .Show();
        }                
    }
}

