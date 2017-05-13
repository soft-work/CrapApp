using System.Threading;
using System.Xml.Linq;

namespace Soft.Crap.IO
{
    public interface PortableSourceWriter
    {
        void WriteSourceXml
        (
            ReaderWriterLockSlim sourceLock,
            XDocument sourceXml            
        );
    }
}
