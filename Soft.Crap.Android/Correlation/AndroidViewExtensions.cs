using System;

using Android.Views;

using Java.Lang;

namespace Soft.Crap.Android.Correlation
{
    public static class AndroidViewExtensions
    {
        public static string GetCorrelationTag
        (
            this View correlatedView
        )
        {
            object viewTag = correlatedView.Tag;

            if (viewTag == null)
            {
                throw new NullReferenceException(nameof(correlatedView.Tag));
            }

            ICharSequence correlationTag = viewTag as ICharSequence;

            if (correlationTag == null)
            {
                throw new InvalidCastException
                (
                    string.Format("'{0}' should be '{1}' but it is of type '{2}'.",
                                  nameof(correlationTag),
                                  typeof(ICharSequence).FullName,
                                  viewTag.GetType().FullName)
                );
            }

            return correlationTag.ToString();
        }        
    }
}
