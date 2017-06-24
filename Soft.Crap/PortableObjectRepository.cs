using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Soft.Crap.Correlation;
using Soft.Crap.Exceptions;
using Soft.Crap.IO;
using Soft.Crap.Logging;
using Soft.Crap.Objects;
using Soft.Crap.Sources;

namespace Soft.Crap
{
    public static class PortableObjectRepository<T>
    {        
        private static readonly ConcurrentDictionary<string, PortableBaseSource> _cachedSources 
            = new ConcurrentDictionary<string, PortableBaseSource>();

        private static readonly List<PortableBaseObject> _cachedObjects = new List<PortableBaseObject>();

        private static readonly Queue<PortableBaseObject> _pendingObjects = new Queue<PortableBaseObject>();

        private static Func<T> _getUiContext;
        private static Action<T, Exception> _showExceptionAndExit;
        private static PortableFileDescriber _fileObjectDescriber;
        private static Func<PortableContextLogger, Action<int>, Task> _objectLoadingTask;        

        public static bool HasUnreadSourceChanges { private set; get; }

        public static void RegisterPlatformSpecific
        (
            Func<T> getUiContext,
            Action<T, Exception> showExceptionAndExit,
            PortableFileDescriber fileObjectDescriber,
            Func<PortableContextLogger, Action<int>, Task> objectLoadingTask
        )
        {
            _getUiContext = getUiContext;
            _showExceptionAndExit = showExceptionAndExit;
            _fileObjectDescriber = fileObjectDescriber;
            _objectLoadingTask = objectLoadingTask;            
        }        

        public static async Task RefreshObjectCacheAsync
        (
            PortableContextLogger contextLogger,
            Action<int> updateCount
        )        
        {
            _objectLoadingTask.ThrowIfUnitialised(nameof(_objectLoadingTask),
                                                  nameof(RefreshObjectCacheAsync),                                                  
                                                  nameof(RegisterPlatformSpecific));
            await Task.Run
            (
                async () =>

                {
                    Parallel.ForEach
                    (
                        _cachedSources,

                        cachedSource => cachedSource.Value.ClearObjects()
                    );

                    _cachedObjects.Clear();

                    await _objectLoadingTask(contextLogger,
                                             updateCount);

                    HasUnreadSourceChanges = true;                    
                }
            );
        }

        public static async Task LoadFileObjectsAsync
        (
            PortableContextLogger contextLogger,
            Action<int> updateCount,
            string sourceFile,
            PortableFileEnumerator fileEnumerator,
            PortableFileDescriber fileDescriber,
            Func<PortableDirectorySource, string, PortableFileObject> createObject
        )
        {
            await Task.Run
            (
                async () =>

                {
                    // http://blog.stephencleary.com/2012/07/dont-block-on-async-code.html                    

                    Task<IEnumerable<KeyValuePair<string, IEnumerable<string>>>> filesByProviders
                        = fileEnumerator.GetFilesByProvidersAsync();

                    Task<IReadOnlyDictionary<string, PortableSourceData>> sourceData
                        = PortableSourceRepository.LoadSourceDataAsync(contextLogger);

                    // http://stackoverflow.com/questions/6123406/waitall-vs-whenall

                    await Task.WhenAll(filesByProviders,
                                       sourceData);
                    
                    var objectsLock = new ReaderWriterLockSlim();                    

                    Parallel.ForEach // TODO partitioning?
                    (
                        filesByProviders.Result,

                        filesByProvider =>
                        {
                            string providerName = filesByProvider.Key;

                            foreach(string filePath in filesByProvider.Value)
                            {
                                string fileDirectory = Path.GetDirectoryName(filePath);

                                PortableDirectorySource directorySource;

                                PortableBaseSource cachedSource;
                                if (_cachedSources.TryGetValue(fileDirectory,
                                                               out cachedSource))
                                {
                                    directorySource = cachedSource as PortableDirectorySource;

                                    if (directorySource == null)
                                    {
                                        throw new InvalidCastException
                                        (
                                            string.Format("'{0}' should be '{1}' but it is of type '{2}'.",
                                                          nameof(cachedSource),
                                                          typeof(PortableDirectorySource).FullName,
                                                          cachedSource.GetType().FullName)
                                        );
                                    }
                                }
                                else
                                {
                                    directorySource = new PortableDirectorySource(providerName,
                                                                                  fileDirectory);
                                    PortableSourceData sourceDatum;
                                    if (sourceData.Result.TryGetValue(fileDirectory,
                                                                      out sourceDatum))
                                    {
                                        directorySource.IsEnabled = sourceDatum.IsEnabled;
                                    }
                                    else
                                    {
                                        directorySource.IsEnabled = (directorySource.DirectoryPath.Contains(".thumbnail") == false) &&
                                                                    (directorySource.DirectoryPath.Contains(".imagecache") == false);
                                    }
                                                                            
                                    _cachedSources.TryAdd(fileDirectory,
                                                          directorySource);
                                }
                                
                                AddFileObject(filePath,
                                              directorySource,
                                              objectsLock,
                                              createObject);

                                updateCount?.Invoke(_cachedObjects.Count);                            
                            }
                        }
                    );
                }
            );
        }        

        public static IEnumerable<O> GetEnabledObjects<O>()
        {
            HasUnreadSourceChanges = false;

            IEnumerable<O> enabledObjects = _cachedObjects.Where
            (
                cachedObject => cachedObject.ObjectSource.IsEnabled
            )
            .Cast<O>();

            return enabledObjects;
        }

        public static IEnumerable<PortableBaseSource> GetObjectSources()
        {
            HasUnreadSourceChanges = false;

            return _cachedSources.Values;
        }

        public static PortableBaseObject GetObjectByCorrelationTag
        (
            string correlationTag
        )
        {
            PortableBaseObject foundObject = _cachedObjects.FirstOrDefault
            (
                cachedObject =>
                {
                    PortableCorrelatedEntity correlatedEntity = cachedObject;

                    return (correlatedEntity.CorrelationTag == correlationTag);
                }
            );

            return foundObject;
        }

        public static void PushPendingObject
        (
            PortableBaseObject pendingObject
        )
        {
            _pendingObjects.Enqueue(pendingObject);
        }

        public static PortableBaseObject PopPendingObject()        
        {
            PortableBaseObject pendingObject = (_pendingObjects.Count > 0)
                                             ? _pendingObjects.Dequeue()
                                             : null;
            return pendingObject;
        }        

        public static PortableFileObject AddFileObject
        (
            string filePath,
            Func<PortableDirectorySource, string, PortableFileObject> createObject
        )
        {
            
            string fileDirectory = Path.GetDirectoryName(filePath);

            PortableDirectorySource directorySource = GetObjectSource(fileDirectory) as PortableDirectorySource;

            if (directorySource == null)
            {
                directorySource = new PortableDirectorySource("NEW",
                                                              fileDirectory);                
                _cachedSources.TryAdd(fileDirectory,
                                      directorySource);                                                         

                /*throw new ArgumentException
                (
                    string.Format("Object source representing directory '{0}' not found.",
                                  fileDirectory),

                    nameof(filePath)
                );*/
            }            

            const ReaderWriterLockSlim objectsLock = null;
            PortableFileObject fileObject = AddFileObject(filePath,
                                                          directorySource,
                                                          objectsLock,
                                                          createObject);
            return fileObject;
        }

        private static PortableFileObject AddFileObject
        (
            string filePath,
            PortableDirectorySource directorySource,
            ReaderWriterLockSlim objectsLock,            
            Func<PortableDirectorySource, string, PortableFileObject> createObject
        )
        {
            _objectLoadingTask.ThrowIfUnitialised(nameof(_fileObjectDescriber),
                                                  nameof(AddFileObject),
                                                  nameof(RegisterPlatformSpecific));

            string fileName = Path.GetFileName(filePath);
            DateTime fileTime = _fileObjectDescriber.GetFileCreationTime(filePath);

            PortableFileObject fileObject = createObject(directorySource,
                                                         fileName);
            directorySource.AddObject(fileObject);

            objectsLock?.EnterWriteLock();

            try
            {
                _cachedObjects.Add(fileObject);
            }
            finally
            {
                objectsLock?.ExitWriteLock();
            }

            return fileObject;
        }

        private static PortableBaseSource GetObjectSource
        (
            string correlationTag
        )
        {            
            PortableBaseSource objectSource;
            if (_cachedSources.TryGetValue(correlationTag,
                                           out objectSource) == false)
            {
                objectSource = null;
            }

            return objectSource;
        }

        private static void ShowExceptionAndExit
        (
            Exception exception
        )
        {
            _getUiContext.ThrowIfUnitialised(nameof(_getUiContext),
                                             nameof(ShowExceptionAndExit),
                                             nameof(RegisterPlatformSpecific));
            T uiContext = _getUiContext();

            _showExceptionAndExit.ThrowIfUnitialised(nameof(_showExceptionAndExit),
                                                     nameof(ShowExceptionAndExit),
                                                     nameof(RegisterPlatformSpecific));
            _showExceptionAndExit(uiContext,
                                  exception);
        }
    }
}

