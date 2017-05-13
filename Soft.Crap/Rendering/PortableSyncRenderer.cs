using System;

namespace Soft.Crap.Rendering
{
    public interface PortableSyncRenderer<T>
    {        
        string ObjectDescription { get; }
        int? ObjectDrawable { get; }
        DateTime ObjectTime { get; }

        string SourceDescription { get; }

        string TypeDescription { get; }
        int TypeDrawable { get; }
        int TypeName { get; }

        void EditObject
        (
            T currentContext,            
            int deviceOrientation
        );
    }
}
