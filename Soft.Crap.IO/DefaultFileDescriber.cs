using System;
using System.IO;

namespace Soft.Crap.IO
{
    public class DefaultFileDescriber : PortableFileDescriber
    {
        DateTime PortableFileDescriber.GetFileCreationTime
        (
            string filePath
        )
        {
            DateTime creationTime = File.GetCreationTime(filePath);

            return creationTime;
        }
    }
}
