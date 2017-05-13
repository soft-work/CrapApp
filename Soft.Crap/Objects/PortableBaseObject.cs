using System;

using Soft.Crap.Correlation;
using Soft.Crap.Sources;

namespace Soft.Crap.Objects
{
    public abstract class PortableBaseObject : PortableCorrelatedEntity
    {
        public PortableBaseObject
        (
            PortableBaseSource objectSource
        )
        {
            ObjectSource = objectSource;            
        }

        string PortableCorrelatedEntity.CorrelationTag
        {
            get { return CorrelationTag; }
        }

        protected abstract string CorrelationTag { get; }

        public abstract string ObjectDescription { get; }        
        public PortableBaseSource ObjectSource { get; }
        public abstract DateTime ObjectTime { get; }

        public virtual string SourceDescription
        {
            get
            {
                string sourceDescription = ObjectSource.ProviderName;

                if (ObjectSource.SourceName != null)
                {
                    sourceDescription += " - " + ObjectSource.SourceName;
                }

                return sourceDescription;
            }
        }

        public abstract string TypeDescription { get; }        
    }
}
