using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;

namespace Soft.Crap.Logging
{    
    internal abstract class AbstractContextLogger : PortableContextLogger
    {
        protected const int StackTraceIndex = 3;

        void PortableContextLogger.LogDebug
        (
            string logFormat,
            params object[] logArguments
        )
        {

#if (DEBUG)
            string logTag = GetLogTag(GetCallingMethodName());

            LogDebug(logTag,
                     logFormat,
                     logArguments);
#endif
        }

        void PortableContextLogger.LogError
        (
            Exception exception
        )
        {
            string logTag = GetLogTag(GetCallingMethodName());

            string logError =
#if (DEBUG)
            exception.ToString();
#else
            exception.Message;
#endif

            LogError(logTag,
                     logError);
        }

        protected abstract string LogLabel { get; }

        protected abstract void LogDebug
        (
            string logTag,
            string logFormat,
            params object[] logArguments
        );

        protected abstract void LogError
        (
            string logTag,
            string logError
        );
                
        protected string GetLogTag
        (
            string methodName
        )
        {
            string logTag = string.Format("{0} {1} [{2}]",
                                          LogLabel,
                                          methodName,
                                          Thread.CurrentThread.Name);
            return logTag;
        }

        protected string GetCallingMethodName()
        {
            var stackTrace = new StackTrace();

            StackFrame stackFrame = stackTrace.GetFrame(StackTraceIndex);
            MethodBase stackMethod = stackFrame.GetMethod();
            Type loggingType = stackMethod.DeclaringType;

            string methodName = string.Format("{0}.{1}",
                                              loggingType.Name,
                                              stackMethod.Name);
            return methodName;
        }       
    }
}

