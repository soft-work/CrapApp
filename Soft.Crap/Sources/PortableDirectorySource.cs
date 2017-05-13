using System.IO;

namespace Soft.Crap.Sources
{
    public class PortableDirectorySource : PortableBaseSource
    {        
        public PortableDirectorySource
        (
            string providerName,
            string directoryPath
        )
        : base
        (
            providerName
        )
        {
            DirectoryPath = directoryPath;            
            FolderName = Path.GetFileName(DirectoryPath);
        }

        protected override string CorrelationTag        
        {
            get { return DirectoryPath; }
        }

        public override string SourceName
        {
            set { base.SourceName = value; }

            get { return base.SourceName ?? FolderName; }
        }

        public override string SourceDetails
        {
            get { return DirectoryPath; }
        }

        public string DirectoryPath { get; }

        public string FolderName { get; }
    }
}
