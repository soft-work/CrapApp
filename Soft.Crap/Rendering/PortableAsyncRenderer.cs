using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Soft.Crap.Correlation;

namespace Soft.Crap.Rendering
{
    public interface PortableAsyncRenderer<T>
    {        
        Task<IReadOnlyDictionary<string, object>> GetAttributesAsync
        (
            PortableCorrelatedEntity correlatedEntity,
            Func<string> correlationTag
        );

        Task<T> GetThumbnailAsync
        (
            PortableCorrelatedEntity correlatedEntity,
            Func<string> correlationTag,
            int viewWidth,
            int viewHeight//,
            //T reusedThumbnail
        );
    }
}
