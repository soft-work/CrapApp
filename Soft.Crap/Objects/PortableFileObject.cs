using System;
using System.IO;

using Soft.Crap.IO;
using Soft.Crap.Sources;

namespace Soft.Crap.Objects
{
    public abstract class PortableFileObject : PortableBaseObject
    {
        public PortableFileObject
        (
            PortableDirectorySource directorySource,
            string fileName,
            PortableFileDescriber fileDescriber
        )
        : base
        (
            directorySource
        )
        {
            DirectorySource = directorySource;

            FileExtension = Path.GetExtension(fileName).TrimStart('.');      
            FileName = Path.GetFileNameWithoutExtension(fileName);
            FilePath = Path.Combine(directorySource.DirectoryPath,
                                    fileName);

            FileTime = fileDescriber.GetFileCreationTime(FilePath);
        }        

        protected override string CorrelationTag
        {
            get { return FilePath; }
        }

        public PortableDirectorySource DirectorySource { get; }        

        public string FileExtension { get; }
        public string FileName { get; }
        public string FilePath { get; }
        public DateTime FileTime { get; }

        public override string ObjectDescription
        {
            get { return FileName; }
        }

        public override DateTime ObjectTime
        {
            get { return FileTime; }
        }

        public override string TypeDescription
        {
            get { return FileExtension.ToUpper(); }
        }
    }
}
