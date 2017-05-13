using System.Threading;
using System.Xml.Linq;

namespace Soft.Crap.IO
{
    public interface PortableSourceReader
    {
        XDocument ReadSourceXml
        (
            ReaderWriterLockSlim sourceLock
        );
    }
}
