#if (DEBUG)
   #define THROW
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

using Soft.Crap.Correlation;
using Soft.Crap.Exceptions;
using Soft.Crap.IO;
using Soft.Crap.Logging;
using Soft.Crap.Sources;

namespace Soft.Crap
{
    public static class PortableSourceRepository
    {
        private const string SourcesElementName = "ObjectSources";
        private const string SourceElementName = "ObjectSource";
        private const string SourceIdAttributeName = "id";
        private const string SourceEnabledAttributeName = "enabled";

        private static readonly ReaderWriterLockSlim _sourceLock = new ReaderWriterLockSlim();
        
        private static Func<PortableContextLogger, PortableSourceReader> _sourceReaderFactory;
        private static Func<PortableContextLogger, PortableSourceWriter> _sourceWriterFactory;                

        public static void RegisterPlatformSpecific
        (            
            Func<PortableContextLogger, PortableSourceReader> sourceReaderFactory,
            Func<PortableContextLogger, PortableSourceWriter> sourceWriterFactory
        )
        {            
            _sourceReaderFactory = sourceReaderFactory;
            _sourceWriterFactory = sourceWriterFactory;
        }        

        public static async Task SaveSourceDataAsync
        (
            IEnumerable<PortableBaseSource> objectSources,
            PortableContextLogger contextLogger
        )
        {
            await Task.Run
            (
                () =>

                {
                    _sourceWriterFactory.ThrowIfUnitialised(nameof(_sourceWriterFactory),
                                                            nameof(SaveSourceDataAsync),
                                                            nameof(RegisterPlatformSpecific));

                    IEnumerable<XElement> sourceElements = from objectSource
                                                           in objectSources
                                                           select new XElement
                    (
                        SourceElementName,

                        new XAttribute(SourceIdAttributeName,
                                       ((PortableCorrelatedEntity)objectSource).CorrelationTag),

                        new XAttribute(SourceEnabledAttributeName,
                                       objectSource.IsEnabled)
                    );

                    var xmlDeclaration = new XDeclaration("1.0",
                                                          "utf-8",
                                                          "yes");
                    var sourceXml = new XDocument
                    (
                        xmlDeclaration,
                        new XElement(SourcesElementName,
                                     sourceElements)
                    );

                    PortableSourceWriter sourceWriter = _sourceWriterFactory(contextLogger);
                                        
                    sourceWriter.WriteSourceXml(_sourceLock,
                                                sourceXml);
                }
            );
        }        

        public static async Task<IReadOnlyDictionary<string, PortableSourceData>> LoadSourceDataAsync
        (            
            PortableContextLogger contextLogger
        )
        {
            return await Task.Run
            (
                () =>

                {
                    IReadOnlyDictionary<string, PortableSourceData> sourceData
                        = new Dictionary<string, PortableSourceData>();

                    PortableSourceReader sourceReader = _sourceReaderFactory(contextLogger);

                    XDocument sourceXml = sourceReader.ReadSourceXml(_sourceLock);

                    if (sourceXml == null)
                    {
                        return sourceData;
                    }

                    try
                    {
                        XElement sourcesElement = sourceXml.Element(SourcesElementName);

                        if (sourcesElement == null)
                        {
                            throw new XmlException(SourcesElementName);
                        }

                        IEnumerable<XElement> sourceElements
                            = from sourceElement
                              in sourcesElement.Elements()
                              where (sourceElement.Name == SourceElementName)
                              select sourceElement;

                        sourceData = sourceElements.ToDictionary
                        (
                            sourceElement =>
                            {
                                XAttribute idAttribute = sourceElement.Attribute(SourceIdAttributeName);

                                if (idAttribute == null)
                                {
                                    throw new XmlException(SourceIdAttributeName);
                                }

                                string sourceKey = idAttribute.Value;

                                return sourceKey;
                            },

                            sourceElement =>
                            {
                                XAttribute enabledAttribute = sourceElement.Attribute(SourceEnabledAttributeName);

                                if (enabledAttribute == null)
                                {
                                    throw new XmlException(SourceEnabledAttributeName);
                                }

                                bool isEnabled = bool.Parse(enabledAttribute.Value);

                                var sourceValue = new PortableSourceData(isEnabled);

                                return sourceValue;
                            }
                        );
                    }
                    catch(Exception exception)
                    {
                        contextLogger.LogError(exception);
#if (THROW)
                        throw;
#endif
                    }

                    return sourceData;
                }
            );
        }
    }
}

