using Soft.Crap.Logging;
using Soft.Crap.IO;

namespace Soft.Crap.Android
{
    public static class AndroidSourceRepository
    {        
        public static PortableSourceReader CreateSourceReader
        (
            PortableContextLogger contextLogger
        )
        {
            string sourceFile = AndroidCrapApplication.GetSourceFilePath();

            PortableSourceReader sourceReader = new DefaultSourceReader(contextLogger,
                                                                        sourceFile);
            return sourceReader;
        }

        public static PortableSourceWriter CreateSourceWriter
        (
            PortableContextLogger contextLogger
        )
        {
            string sourceFile = AndroidCrapApplication.GetSourceFilePath();

            PortableSourceWriter sourceWriter = new DefaultSourceWriter(contextLogger,
                                                                        sourceFile);

            return sourceWriter;
        }        
    }      
}
