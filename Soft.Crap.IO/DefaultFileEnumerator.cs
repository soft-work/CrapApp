using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Soft.Crap.IO
{
    public class DefaultFileEnumerator : PortableFileEnumerator
    {
        private readonly IReadOnlyDictionary<string, string> _fileProviders;
        private readonly string[] _fileExtensions;        

        public DefaultFileEnumerator
        (
            IReadOnlyDictionary<string, string> fileProviders,
            params string[] fileExtensions
        )
        {
            _fileProviders = fileProviders;
            _fileExtensions = fileExtensions;
        }

        async Task<IEnumerable<KeyValuePair<string, IEnumerable<string>>>> PortableFileEnumerator.GetFilesByProvidersAsync()
        {
            return await Task.Run
            (
                () => GetFilesByProvidersSync()
            );
        }

        private IEnumerable<KeyValuePair<string, IEnumerable<string>>> GetFilesByProvidersSync()
        {
            string[] searchPatterns = 
            (
                from fileExtension
                in _fileExtensions
                select string.Format("*.{0}",
                                     fileExtension)
            ).ToArray();

            var filesByProviders = new List<KeyValuePair<string, IEnumerable<string>>>();

            var listLock = new ReaderWriterLockSlim();

            Parallel.ForEach
            (
                _fileProviders,

                fileProvider =>
                {
                    var filesByProvider = new KeyValuePair<string, IEnumerable<string>>
                    (
                        fileProvider.Key,

                        searchPatterns.AsParallel().SelectMany
                        (
                            (searchPattern) => Directory.EnumerateFiles(fileProvider.Value,
                                                                        searchPattern,
                                                                        SearchOption.AllDirectories)
                        )
                    );

                    listLock.EnterWriteLock();

                    try
                    {
                        filesByProviders.Add
                        (
                            new KeyValuePair<string, IEnumerable<string>>(filesByProvider.Key,
                                                                          filesByProvider.Value)
                        );
                    }
                    finally
                    {
                        listLock.ExitWriteLock();
                    }
                }
            );

            return filesByProviders;
        }
    }
}
