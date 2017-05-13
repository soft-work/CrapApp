using System;

namespace Soft.Crap.Exceptions
{    
    public static class UnhandledExceptionHandler
    {
        public static void Activate()
        {
            AppDomain.CurrentDomain.UnhandledException += UnhandledException;
        }
        
        private static void UnhandledException
        (
            object sender,
            UnhandledExceptionEventArgs arguments
        )
        {
            Exception unhandledException = (Exception)arguments.ExceptionObject;

            UnobservedExceptionHandler.HandleException(unhandledException);
            
            //if (arguments.IsTerminating == false)
            {
                Environment.Exit(-1);
            }            
        }
    }
}

