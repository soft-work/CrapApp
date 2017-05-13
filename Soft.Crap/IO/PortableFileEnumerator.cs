using System.Collections.Generic;
using System.Threading.Tasks;

namespace Soft.Crap.IO
{
    public interface PortableFileEnumerator
    {
        Task<IEnumerable<KeyValuePair<string, IEnumerable<string>>>> GetFilesByProvidersAsync();
    }
}
