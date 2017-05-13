using System;

using Android.Views;

namespace Soft.Crap.Android.Adapters
{
    public static class AndroidViewExtensions
    {
        public static T GetParentView<T>
        (
            this View childView
        )
        where T : View
        {
            T parentView = childView.Parent as T;

            if (parentView == null)
            {
                throw new InvalidCastException
                (
                    string.Format("'{0}' should be '{1}' but it is of type '{2}'.",
                                  nameof(parentView),
                                  typeof(T).FullName,
                                  parentView.GetType().FullName)
                );
            }

            return parentView;
        }        
    }
}