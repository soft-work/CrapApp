using System;
using System.Linq;

namespace Soft.Crap.Exceptions
{
    public static class PortableExceptionExtensions
    {
        public static string GetAggregatedMessage
        (
            this Exception exception
        )
        {
            AggregateException aggregateException = exception as AggregateException;

            string aggregatedMessage = GetExceptionDescription(exception);

            if (aggregateException != null)
            {
                aggregatedMessage += aggregateException.InnerExceptions.Aggregate
                (
                    string.Empty,

                    (aggregation, aggregated) =>
                    {
                        return aggregation +
                               Environment.NewLine +
                               Environment.NewLine +
                               GetExceptionDescription(aggregated);
                    }
                );
            }

            return aggregatedMessage;
        }

        private static string GetExceptionDescription
        (
            Exception exception
        )
        {
            string description =
#if DEBUG
            exception.ToString();
#else
            exception.Message;
#endif
            return description;
        }


        /*public static T Cast<T>
        (
            this AggregateException aggregateException
        )
        where T : Exception
        {
            T singleException = null;

            if (aggregateException.InnerExceptions.Count == 1)
            {
                singleException = aggregateException.InnerExceptions.FirstOrDefault
                (
                    exception => (exception is T)
                )
                as T;
            }

            return singleException;
        }*/
    }    
}
