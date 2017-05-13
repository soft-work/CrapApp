using System;

using Android.OS;
using Android.App;
using Android.Graphics;
using Android.Runtime;
using Android.Views;
using Android.Widget;

using Object = Java.Lang.Object;

namespace Soft.Crap.Android.Fragments
{
    public class AndroidBrowseFooterFragment : Fragment
    {
        private static readonly ImageView.ScaleType CropScaleType = ImageView.ScaleType.CenterCrop;
        private static readonly ImageView.ScaleType FitScaleType = ImageView.ScaleType.FitCenter;

        private static ImageView.ScaleType _thumbnailScaleType = null;        

        private Button _gridViewButton;
        private Button _listViewButton;
        private Button _cropThumbnailButton;
        private Button _fitThumbnailButton;

        private AbsListView _abstractBrowseView;

        private Action _viewTypeChanged;
        private Action _scaleTypeChanged;

        Func<int> _getThumbnailWidth;

        public override View OnCreateView
        (
            LayoutInflater inflater,
            ViewGroup container,
            Bundle bundle
        )
        {
            View fragment = inflater.Inflate(Resource.Layout.BrowseFooterFragment,
                                             container,
                                             attachToRoot : false);            
            return fragment;
        }

        public void ConfigureBrowsingFor
        (
            AbsListView abstractBrowseView,
            Func<int> getThumbnailWidth,
            Action viewTypeChanged,
            Action scaleTypeChanged            
        )
        {
            _abstractBrowseView = abstractBrowseView;

            _getThumbnailWidth = getThumbnailWidth;

            _viewTypeChanged = viewTypeChanged;
            _scaleTypeChanged = scaleTypeChanged;            

            _abstractBrowseView.ScrollBarStyle = ScrollbarStyles.InsideInset;
            _abstractBrowseView.FastScrollEnabled = true;
            _abstractBrowseView.FastScrollAlwaysVisible = true;

            AbsListView.IOnScrollListener browseScrollListener = CreateScrollListener();
            _abstractBrowseView.SetOnScrollListener(browseScrollListener);

            _gridViewButton = Activity.FindViewById<Button>(Resource.Id.GridViewButton);
            _gridViewButton.Click -= GridViewButtonClicked;
            _gridViewButton.Click += GridViewButtonClicked;
            _gridViewButton.LongClick -= ModeButtonLongClicked;
            _gridViewButton.LongClick += ModeButtonLongClicked;

            _listViewButton = Activity.FindViewById<Button>(Resource.Id.ListViewButton);
            _listViewButton.Click -= ListViewButtonClicked;
            _listViewButton.Click += ListViewButtonClicked;
            _listViewButton.LongClick -= ModeButtonLongClicked;
            _listViewButton.LongClick += ModeButtonLongClicked;

            _cropThumbnailButton = Activity.FindViewById<Button>(Resource.Id.CropThumbnailButton);
            _cropThumbnailButton.Click -= CropThumbnailButtonClicked;
            _cropThumbnailButton.Click += CropThumbnailButtonClicked;
            _cropThumbnailButton.LongClick -= ModeButtonLongClicked;
            _cropThumbnailButton.LongClick += ModeButtonLongClicked;

            _fitThumbnailButton = Activity.FindViewById<Button>(Resource.Id.FitThumbnailButton);
            _fitThumbnailButton.Click -= FitThumbnailButtonClicked;
            _fitThumbnailButton.Click += FitThumbnailButtonClicked;
            _fitThumbnailButton.LongClick -= ModeButtonLongClicked;
            _fitThumbnailButton.LongClick += ModeButtonLongClicked;            
        }

        public void SetScaleType
        (
            ImageView.ScaleType thumbnailScaleType
        )        
        {
            _thumbnailScaleType = thumbnailScaleType;
        }

        public ImageView.ScaleType GetScaleType()
        {
            return _thumbnailScaleType;
        }
  
        public void ShowPopupMenu
        (
            int popupResource,
            Action<PopupMenu> initializeItems = null
        )        
        {
            // http://stackoverflow.com/questions/13444594/how-can-i-change-the-position-of-where-my-popup-menu-pops-up

            ViewGroup viewGroup = (ViewGroup)Activity.Window.DecorView;

            var menuAncor = new View(Activity);
            menuAncor.LayoutParameters = new ViewGroup.LayoutParams(1, 1);
            menuAncor.SetBackgroundColor(Color.Transparent);

            viewGroup.AddView(menuAncor);            

            var popupMenu = new PopupMenu(Activity,
                                          menuAncor,
                                          GravityFlags.NoGravity);

            popupMenu.SetOnMenuItemClickListener
            (
                new PopupMenuItemClickListener
                (
                    popupMenu,

                    _abstractBrowseView,

                    (checkableResource, initalizeItems) => ShowPopupMenu(checkableResource,
                                                                         initalizeItems),
                    (viewType) => SwitchViewTypeTo(viewType),

                    (scaleType) => SwitchThumbnailScalingTo(scaleType)
                )
            );

            popupMenu.SetOnDismissListener
            (
                new PopupMenuDismissListener(viewGroup,
                                             menuAncor)
            );    

            popupMenu.Inflate(popupResource);

            int thumbnailWidth = _getThumbnailWidth();

            menuAncor.SetX(thumbnailWidth * 1.5f);
            menuAncor.SetY(viewGroup.Height / 2);

            initializeItems?.Invoke(popupMenu);

            popupMenu.Show();
        }

        private class PopupMenuDismissListener : Object, PopupMenu.IOnDismissListener
        {
            private readonly ViewGroup _viewGroup;
            private readonly View _ancorView;

            public PopupMenuDismissListener
            (
                ViewGroup viewGroup,
                View ancorView
            )
            {
                _viewGroup = viewGroup;
                _ancorView = ancorView;
            }

            void PopupMenu.IOnDismissListener.OnDismiss
            (
                PopupMenu popupMenu
            )
            {
                _viewGroup.RemoveView(_ancorView);
            }
        }

        private void GridViewButtonClicked
        (
            object sender,
            EventArgs arguments
        )
        {
            ShowModeButtonHint(sender);

            SwitchViewTypeTo(typeof(GridView));
        }

        private void ListViewButtonClicked
        (
            object sender,
            EventArgs arguments
        )
        {
            ShowModeButtonHint(sender);

            SwitchViewTypeTo(typeof(ListView));
        }

        private void CropThumbnailButtonClicked
        (
            object sender,
            EventArgs arguments
        )
        {
            ShowModeButtonHint(sender);

            SwitchThumbnailScalingTo(CropScaleType);
        }

        private void FitThumbnailButtonClicked
        (
            object sender,
            EventArgs arguments
        )
        {
            ShowModeButtonHint(sender);

            SwitchThumbnailScalingTo(FitScaleType);
        }

        private void SwitchViewTypeTo
        (
            Type targetType
        )
        {
            Type currentType = _abstractBrowseView.GetType();

            if (targetType != currentType)
            {
                _viewTypeChanged();                
            }
        }

        private void SwitchThumbnailScalingTo
        (
            ImageView.ScaleType targetType
        )
        {
            if (_thumbnailScaleType != targetType)
            {
                _thumbnailScaleType = targetType;

                _scaleTypeChanged();                
            }
        }

        private void ModeButtonLongClicked
        (
            object sender,
            View.LongClickEventArgs arguments
        )
        {
            ShowModeButtonHint(sender);

            arguments.Handled = true;
        }

        private void ShowModeButtonHint
        (
            object sender
        )
        {
            View view = (View)sender;

            Toast.MakeText
            (
                Activity,
                view.ContentDescription,
                ToastLength.Short
            )
            .Show();
        }        

        private AbsListView.IOnScrollListener CreateScrollListener()
        {
            TextView footerScrollText = Activity.FindViewById<TextView>
            (
                Resource.Id.FooterScrollText
            );

            string scrollTextFormat = GetString(Resource.String.ScrollTextFormat);

            AbsListView.IOnScrollListener scrollListener = new ScrollListener
            (
                updateScroll : (firstVisibleItem,
                                visibleItemCount,
                                totalItemCount) =>
                {
                    footerScrollText.Text = (visibleItemCount == 0)
                                          ? string.Empty
                                          : string.Format(scrollTextFormat,
                                                          firstVisibleItem + 1,
                                                          firstVisibleItem + visibleItemCount,
                                                          totalItemCount);
                }
            );

            return scrollListener;
        }

        private class ScrollListener : Object, AbsListView.IOnScrollListener
        {
            private readonly Action<int, int, int> _updateScroll;

            public ScrollListener
            (
                Action<int, int, int> updateScroll
            )
            {
                _updateScroll = updateScroll;
            }

            void AbsListView.IOnScrollListener.OnScroll
            (
                AbsListView objectListView,
                int firstVisibleItem,
                int visibleItemCount,
                int totalItemCount
            )
            {
                _updateScroll(firstVisibleItem,
                              visibleItemCount,
                              totalItemCount);
            }

            void AbsListView.IOnScrollListener.OnScrollStateChanged
            (
                AbsListView objectListView,
                [GeneratedEnum] ScrollState scrollState
            )
            { }
        }

        private class PopupMenuItemClickListener : Object, PopupMenu.IOnMenuItemClickListener
        {
            private readonly PopupMenu _popupMenu;
            private readonly AbsListView _abstractView;

            private readonly Action<int, Action<PopupMenu>> _showNestedPopup;
            private readonly Action<Type> _switchViewTypeTo;
            private readonly Action<ImageView.ScaleType> _switchImageScalingTo;

            public PopupMenuItemClickListener
            (
                PopupMenu popupMenu,
                AbsListView abstractView,
                Action<int, Action<PopupMenu>> showNestedPopup,
                Action<Type> switchViewTypeTo,
                Action<ImageView.ScaleType> switchImageScalingTo
            )
            {
                _popupMenu = popupMenu;
                _abstractView = abstractView;
                _showNestedPopup = showNestedPopup;
                _switchViewTypeTo = switchViewTypeTo;
                _switchImageScalingTo = switchImageScalingTo;
            }

            bool PopupMenu.IOnMenuItemClickListener.OnMenuItemClick
            (
                IMenuItem menuItem
            )
            {
                switch(menuItem.ItemId)
                {
                    case Resource.Id.ViewTypeItem:
                    {
                        FireCheckablePopup
                        (
                            Resource.Menu.ViewTypeCheckableMenu,

                            (checkablePopup) =>
                            {
                                InitializeCheckableMenuItem(checkablePopup.Menu,
                                                            Resource.Id.GridViewItem,
                                                            _abstractView is GridView);

                                InitializeCheckableMenuItem(checkablePopup.Menu,
                                                            Resource.Id.ListViewItem,
                                                            _abstractView is ListView);
                            }
                        );

                        break;
                    }

                    case Resource.Id.ScaleTypeItem:
                    {
                        FireCheckablePopup
                        (
                            Resource.Menu.ScaleTypeCheckableMenu,

                            (checkablePopup) =>
                            {
                                InitializeCheckableMenuItem(checkablePopup.Menu,
                                                            Resource.Id.CropImageItem,
                                                            _thumbnailScaleType == CropScaleType);

                                InitializeCheckableMenuItem(checkablePopup.Menu,
                                                            Resource.Id.FitImageItem,
                                                            _thumbnailScaleType == FitScaleType);
                            }
                        );                            

                        break;
                    }

                    case Resource.Id.GridViewItem:
                    {
                        _switchViewTypeTo(typeof(GridView));

                        break;
                    }

                    case Resource.Id.ListViewItem:
                    {
                        _switchViewTypeTo(typeof(ListView));

                        break;
                    }

                    case Resource.Id.CropImageItem:
                    {
                        _switchImageScalingTo(CropScaleType);

                        break;
                    }

                    case Resource.Id.FitImageItem:
                    {
                        _switchImageScalingTo(FitScaleType);

                        break;
                    }

                    default:
                    {
                        throw new InvalidOperationException(menuItem.TitleFormatted.ToString());
                    }
                }

                return true;
            }

            private void FireCheckablePopup
            (
                int checkableResource,
                Action<PopupMenu> initalizeItems
            )
            {
                _abstractView.Post
                (
                    () => _showNestedPopup(checkableResource,
                                           initalizeItems)
                );
            }

            private void InitializeCheckableMenuItem
            (
                IMenu checkableMenu,
                int itemId,
                bool isChecked
            )
            {
                IMenuItem menuItem = checkableMenu.FindItem(itemId);

                // https://code.google.com/p/android/issues/detail?id=178709 !!!

                if (isChecked)
                {
                    menuItem.SetChecked(false);
                }                
            }            
        }
    }
}