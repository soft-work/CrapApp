using System;

namespace Soft.Crap.IO
{
    public interface PortableFileDescriber
    {
        DateTime GetFileCreationTime
        (
            string filePath
        );        
    }
}
