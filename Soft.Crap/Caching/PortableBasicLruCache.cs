using System;
using System.Collections.Generic;
using System.Threading;

// http://brett.duncavage.org/2014/02/in-memory-bitmap-caching-with.html
// https://github.com/rdio/tangoandcache

namespace Soft.Crap.Caching
{
    public class PortableBasicLruCache<K, V>
    {
        private readonly int _maxSize;
        private readonly Action<K, V> _entryAdded;
        private readonly Action<K, V, V, bool> _entryRemoved;

        private readonly ReaderWriterLockSlim _cacheLock = new ReaderWriterLockSlim();
        private readonly IDictionary<K, V> _cachedEntries = new Dictionary<K, V>();
        private readonly LinkedList<K> _lruList = new LinkedList<K>();        

        public PortableBasicLruCache
        (
            Action<K, V> entryAdded,
            Action<K, V, V, bool> entryRemoved
        )
        : this(entryAdded,
               entryRemoved,
               maxSize : 0) { }

        public PortableBasicLruCache
        (
            Action<K, V> entryAdded,
            Action<K, V, V, bool> entryRemoved,
            int maxSize
        )
        {            
            _entryAdded = entryAdded;
            _entryRemoved = entryRemoved;
            _maxSize = maxSize;
        }        

        public bool TryGetValue
        (
            K cachedKey,
            out V cachedValue,
            bool updateLru = true
        )
        {
            bool keyFound;

            if (updateLru)
            {
                _cacheLock.EnterWriteLock();
            }
            else
            {
                _cacheLock.EnterReadLock();
            }

            try
            {
                keyFound = (_cachedEntries.TryGetValue(cachedKey,
                                                       out cachedValue));
                if (keyFound && updateLru)
                {
                    _lruList.Remove(cachedKey);
                    _lruList.AddLast(cachedKey);
                }
            }
            finally
            {
                if (updateLru)
                {
                    _cacheLock.ExitWriteLock();
                }
                else
                {
                    _cacheLock.ExitReadLock();
                }                
            }

            return keyFound;
        }        

        public void Add
        (
            K cachedKey,
            V cachedValue
        )
        {
            bool raiseEntryRemovedDueToEviction = false;

            IDictionary<K, V> evictedEntries = null;
            
            K overwrittenKey = default(K);
            V overwrittenValue = default(V);

            _cacheLock.EnterWriteLock();

            try
            {
                if (_cachedEntries.ContainsKey(cachedKey))
                {
                    _lruList.Remove(cachedKey);
                    
                    overwrittenKey = cachedKey;
                    overwrittenValue = _cachedEntries[cachedKey];
                    _cachedEntries[cachedKey] = cachedValue;
                }
                else
                {
                    _cachedEntries.Add(cachedKey,
                                       cachedValue);
                }

                _lruList.AddLast(cachedKey);

                while(CheckEvictionRequired())
                {
                    if (_lruList.Count == 0)
                    {
                        break;
                    }

                    OnWillEvictEntry(_lruList.First.Value,
                                     _cachedEntries[_lruList.First.Value]);

                    evictedEntries = evictedEntries ?? new Dictionary<K, V>();

                    K evictedKey = _lruList.First.Value;

                    evictedEntries[evictedKey] = _cachedEntries[evictedKey];

                    _cachedEntries.Remove(evictedKey);
                    _lruList.RemoveFirst();

                    raiseEntryRemovedDueToEviction = true;
                }
            }
            finally
            {
                _cacheLock.ExitWriteLock();
            }

            OnEntryAdded(cachedKey,
                         cachedValue);

            if (raiseEntryRemovedDueToEviction)
            {
                foreach(K evictedKey in evictedEntries.Keys)
                {
                    OnEntryRemoved(evictedKey,
                                   evictedEntries[evictedKey],
                                   default(V),
                                   isEvicted : true);
                }
            }
            
            OnEntryRemoved(overwrittenKey,
                           overwrittenValue,
                           cachedValue,
                           isEvicted : false);
        }

        public bool ContainsKey
        (
            K searchedKey
        )
        {
            bool containsKey;

            _cacheLock.EnterReadLock();

            try
            {
                containsKey = _cachedEntries.ContainsKey(searchedKey);
            }
            finally
            {
                _cacheLock.ExitReadLock();
            }

            return containsKey;
        }

        public bool Remove
        (
            K removedKey
        )
        {
            V oldValue = default(V);

            bool isRemoved = false;

            _cacheLock.EnterWriteLock();

            try
            {
                if (_cachedEntries.ContainsKey(removedKey))
                {
                    oldValue = _cachedEntries[removedKey];
                }

                _lruList.Remove(removedKey);

                isRemoved = _cachedEntries.Remove(removedKey);
            }
            finally
            {
                _cacheLock.ExitWriteLock();
            }

            if (isRemoved)
            {
                V newValue = default(V);

                OnEntryRemoved(removedKey,
                               oldValue,
                               newValue,
                               isEvicted : false);
            }

            return isRemoved;
        }

        public void Clear()
        {
            K[] removedKeys = null;

            _cacheLock.EnterWriteLock();

            try
            {
                removedKeys = new K[_cachedEntries.Keys.Count];
                _cachedEntries.Keys.CopyTo(removedKeys, 0);
            }
            finally
            {
                _cacheLock.ExitWriteLock();
            }

            if (removedKeys != null)
            {
                foreach (K removedKey in removedKeys)
                {
                    Remove(removedKey);
                }
            }
        }

        public ICollection<K> Keys
        {
            get
            {
                ICollection<K> cachedKeys;

                _cacheLock.EnterReadLock();

                try
                {
                    cachedKeys = _lruList;
                }
                finally
                {
                    _cacheLock.ExitReadLock();
                }

                return cachedKeys;
            }
        }

        public int Count
        {
            get
            {
                int entryCount;

                _cacheLock.EnterReadLock();

                try
                {
                    entryCount = _cachedEntries.Count;
                }
                finally
                {
                    _cacheLock.ExitReadLock();
                }

                return entryCount;
            }
        }

        protected virtual void OnWillEvictEntry
        (
            K key,
            V value
        )
        { }

        protected virtual bool CheckEvictionRequired()
        {
            return Count > _maxSize;
        }        

        protected virtual void OnEntryAdded
        (
            K newKey,
            V newValue
        )
        {
            _entryAdded?.Invoke(newKey,
                                newValue);
        }

        protected virtual void OnEntryRemoved
        (
            K removedKey,
            V oldValue,
            V newValue,
            bool isEvicted
        )
        {            
            _entryRemoved?.Invoke(removedKey,
                                  oldValue,
                                  newValue,
                                  isEvicted);            
        }                        
    }
}