#if (DEBUG)
   #define THROW
#endif

using System;
using System.IO;
using System.Threading;
using System.Xml.Linq;

using Soft.Crap.Logging;

namespace Soft.Crap.IO
{
    public class DefaultSourceReader : PortableSourceReader
    {
        private readonly PortableContextLogger _contextLogger;
        private readonly string _fileName;        

        public DefaultSourceReader
        (
            PortableContextLogger contextLogger,
            string fileName
        )
        {
            _contextLogger = contextLogger;
            _fileName = fileName;
        }

        XDocument PortableSourceReader.ReadSourceXml
        (
            ReaderWriterLockSlim sourceLock
        )
        {
            XDocument sourceXml = null;

            sourceLock.EnterReadLock();

            try
            {
                if (File.Exists(_fileName))
                {
                    /*Debug.Write
                    (
                        string.Format("{0}*****{1}{2}{3}*****{4}",
                                      Environment.NewLine,
                                      Environment.NewLine,
                                      File.ReadAllText(_fileName),
                                      Environment.NewLine,
                                      Environment.NewLine)
                    );*/
                   
                    sourceXml = XDocument.Load(_fileName);
                }
            }
            catch(Exception exception)
            {
                _contextLogger.LogError(exception);
#if (THROW)
                throw;
#endif
            }
            finally
            {
                sourceLock.ExitReadLock();
            };

            return sourceXml;
        }
    }
}
