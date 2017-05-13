using Soft.Crap.IO;
using Soft.Crap.Sources;

namespace Soft.Crap.Objects
{
    public abstract class DefaultFileObject : PortableFileObject
    {
        public DefaultFileObject
        (
            PortableDirectorySource directorySource,
            string fileName
        )
        : base
        (
            directorySource,
            fileName,
            new DefaultFileDescriber()
        )
        { }        
    }
}