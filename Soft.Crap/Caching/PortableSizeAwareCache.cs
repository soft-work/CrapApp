using System;
using System.Threading;

// http://brett.duncavage.org/2014/02/in-memory-bitmap-caching-with.html
// https://github.com/rdio/tangoandcache

namespace Soft.Crap.Caching
{
    public class PortableSizeAwareCache<K, V> : PortableBasicLruCache<K, V>
           where V : PortableSizeAwareEntry
    {
        private readonly long _highWatermark;
        private readonly long _lowWatermark;

        private readonly ReaderWriterLockSlim _cacheLock = new ReaderWriterLockSlim();        
        private long _cacheSize;
        private bool _isFull;

        public PortableSizeAwareCache
        (            
            long highWatermark,
            long lowWatermark,
            Action<K, V> entryAdded,
            Action<K, V, V, bool> entryRemoved
        )
        : base
        (
            entryAdded,
            entryRemoved
        )
        {                        
            if (lowWatermark <= 0)
            {
                throw new ArgumentException(nameof(lowWatermark));
            }

            if (highWatermark < lowWatermark)
            {
                throw new ArgumentException(nameof(highWatermark));
            }            

            _highWatermark = highWatermark;
            _lowWatermark = lowWatermark;
        }

        public long CacheSizeInBytes
        {
            get
            {
                long sizeInBytes;

                _cacheLock.EnterReadLock();

                try
                {
                    sizeInBytes = _cacheSize;
                }
                finally
                {
                    _cacheLock.ExitReadLock();
                }

                return sizeInBytes;
            }
        }

        protected override void OnEntryAdded
        (
            K newKey,
            V newValue
        )
        {
            _cacheLock.EnterWriteLock();

            try
            {
                _cacheSize += newValue.SizeInBytes;
            }
            finally
            {
                _cacheLock.ExitWriteLock();
            }

            base.OnEntryAdded(newKey,
                              newValue);
        }

        protected override void OnEntryRemoved
        (            
            K removedKey,
            V oldValue,
            V newValue,
            bool isEvicted
        )
        {
            base.OnEntryRemoved(removedKey,
                                oldValue,
                                newValue,
                                isEvicted);
            
            if (isEvicted == false)
            {
                return;
            }

            _cacheLock.EnterWriteLock();

            try
            {
                _cacheSize -= oldValue.SizeInBytes;
            }
            finally
            {
                _cacheLock.ExitWriteLock();
            }
        }

        protected override void OnWillEvictEntry
        (
            K evictedKey,
            V evictedValue
        )
        {
            _cacheLock.EnterWriteLock();

            try
            {
                _cacheSize -= evictedValue.SizeInBytes;
            }
            finally
            {
                _cacheLock.ExitWriteLock();
            }
        }

        protected override bool CheckEvictionRequired()
        {
            bool evictionRequired = false;

            _cacheLock.EnterWriteLock();

            try
            {
                if (_cacheSize > _highWatermark)
                {
                    _isFull = true;

                    evictionRequired = true;                    
                }
                else if (_isFull && (_cacheSize > _lowWatermark))
                {
                    evictionRequired = true;
                }
                else
                {
                    _isFull = false;
                }                
            }
            finally
            {
                _cacheLock.ExitWriteLock();
            }

            return evictionRequired;
        }
    }
}