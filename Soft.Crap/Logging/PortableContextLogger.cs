using System;

namespace Soft.Crap.Logging
{
    public interface PortableContextLogger
    {
        void LogDebug
        (
            string format,
            params object[] arguments
        );        

        void LogError
        (
            Exception exception
        );                
    }
}

