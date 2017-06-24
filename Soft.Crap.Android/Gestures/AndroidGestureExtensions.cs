using Android.Views;

using Object = Java.Lang.Object;

namespace Soft.Crap.Android.Gestures
{
    public static class AndroidGestureExtensions
    {        
        public static void DragOver
        (
            this View draggedView,
            View backgroudView
        )
        {
            View.IOnDragListener dragListener = new DragListener(draggedView);
            backgroudView.SetOnDragListener(dragListener);

            var shadowBuilder = new View.DragShadowBuilder(draggedView);

            draggedView.StartDrag // StartDragAndDrop
            (
                null, // the data to be dragged
                shadowBuilder,
                null, // no need to use local data
                0  // flags (not currently used, set to 0)
            );
        }

        // https://developer.android.com/guide/topics/ui/drag-drop.html#AboutDragListeners

        private class DragListener : Object, View.IOnDragListener
        {
            private readonly View _draggedView;            

            private float _dropX;
            private float _dropY;

            public DragListener
            (
                View draggedView
            )
            {
                _draggedView = draggedView;                
            }

            bool View.IOnDragListener.OnDrag
            (
                View backView,
                DragEvent dragEvent
            )
            {
                switch(dragEvent.Action)
                {
                    case DragAction.Drop:
                    {
                        _dropX = dragEvent.GetX();
                        _dropY = dragEvent.GetY();                            

                        break;
                    }

                    case DragAction.Ended:
                    {                        
                        float halfWidth = backView.Width / 2;
                        float halfHeight = backView.Height / 2;

                        float backX = (_dropX - halfWidth) * backView.ScaleX + halfWidth - 
                                      (_draggedView.Width / 2);

                        float backY = (_dropY - halfHeight) * backView.ScaleY + halfHeight -
                                      (_draggedView.Height / 2);

                        _draggedView.SetX(backX);
                        _draggedView.SetY(backY);

                        backView.SetOnDragListener(null);

                        break;
                    }
                }

                return true;
            }
        }
    }
}