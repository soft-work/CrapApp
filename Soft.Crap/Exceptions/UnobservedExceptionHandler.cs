using System;
using System.Threading.Tasks;

using Soft.Crap.Logging;

namespace Soft.Crap.Exceptions
{
    public static class UnobservedExceptionHandler
    {        
        private static PortableContextLogger _exceptionLogger;
        private static Action _exceptionClear;
        private static Action<string> _exceptionNotification;   

        static UnobservedExceptionHandler()
        {
            TaskScheduler.UnobservedTaskException += HandleException;
        }

        public static void RegisterPlatformSpecific
        (
            PortableContextLogger exceptionLogger,
            Action exceptionClear,
            Action<string> exceptionNotification            
        )
        {
            _exceptionLogger = exceptionLogger 
                             ?? throw new ArgumentNullException(nameof(exceptionLogger));

            _exceptionClear = exceptionClear;
            _exceptionNotification = exceptionNotification;         
        }
        
        private static void HandleException
        (
            object sender,
            UnobservedTaskExceptionEventArgs arguments
        )
        {
            HandleException(arguments.Exception);

            arguments.SetObserved();
        }

        public static void HandleException
        (
            Exception unhandledException
        )
        {
            try
            {
                _exceptionClear.ThrowIfUnitialised(nameof(_exceptionClear),
                                                   nameof(HandleException),
                                                   nameof(RegisterPlatformSpecific));
                _exceptionClear();

                _exceptionLogger.LogError(unhandledException);

                _exceptionNotification.ThrowIfUnitialised(nameof(_exceptionNotification),
                                                          nameof(HandleException),
                                                          nameof(RegisterPlatformSpecific));

                _exceptionNotification(unhandledException.GetAggregatedMessage());
            }
            catch(Exception exception)
            {
                _exceptionClear?.Invoke();

                _exceptionLogger.LogError(exception);                
            }            
        }     
    }    
}
