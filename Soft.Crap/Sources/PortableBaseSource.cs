using System.Collections.Generic;

using Soft.Crap.Correlation;
using Soft.Crap.Objects;

namespace Soft.Crap.Sources
{
    public abstract class PortableBaseSource : PortableCorrelatedEntity
    {        
        private readonly List<PortableBaseObject> _sourceObjects = new List<PortableBaseObject>();

        public PortableBaseSource
        (
            string providerName
        )
        {
            ProviderName = providerName;            
        }

        string PortableCorrelatedEntity.CorrelationTag
        {
            get { return CorrelationTag; }
        }

        protected abstract string CorrelationTag { get; }

        public string ProviderName { get; }

        public bool IsEnabled { set; get; }        

        public virtual string SourceName { set; get; }

        public abstract string SourceDetails { get; }

        public IReadOnlyList<PortableBaseObject> SourceObjects
        {
            get { return _sourceObjects; }
        }

        public void AddObject
        (
            PortableBaseObject sourceObject
        )
        {
            _sourceObjects.Add(sourceObject);
        }

        public void ClearObjects()
        {
            _sourceObjects.Clear();
        }
    }
}