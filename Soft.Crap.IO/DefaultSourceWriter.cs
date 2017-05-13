using System.Threading;
using System.Xml.Linq;

using Soft.Crap.Logging;

namespace Soft.Crap.IO
{
    public class DefaultSourceWriter : PortableSourceWriter
    {
        private readonly PortableContextLogger _contextLogger;
        private readonly string _fileName;

        public DefaultSourceWriter
        (
            PortableContextLogger contextLogger,
            string fileName
        )
        {
            _contextLogger = contextLogger;
            _fileName = fileName;
        }

        void PortableSourceWriter.WriteSourceXml
        (
            ReaderWriterLockSlim sourceLock,
            XDocument sourceXml
        )
        {
            sourceLock.EnterWriteLock();            

            try
            {                
                // Beware of 'Stream' version - doesn't seem to work as expected!
                sourceXml.Save(_fileName,
                               SaveOptions.None);
            }
            finally
            {
                sourceLock.ExitWriteLock();
            }
        }
    }
}
