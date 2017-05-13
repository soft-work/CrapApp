using System;
using System.Linq;

namespace Soft.Crap.Exceptions
{
    internal static class MulticastDelegateExtensions
    {
        internal static void ThrowIfUnitialised
        (
            this MulticastDelegate checkedDelegate,
            string delegateName,
            string callerName,
            string predecessorName
        )
        {
            bool isDelegateUninitalised = checkedDelegate.GetInvocationList().Any
            (
                checkedInvocation => (checkedInvocation == null)
            );

            if (isDelegateUninitalised)
            {
                throw new InvalidOperationException
                (
                    string.Format("'{0}' is null. '{1}' may be called without previous '{2}' call.",
                                  delegateName,
                                  callerName,
                                  predecessorName)
                );
            }
        }        
    }
}
