using System;
using System.Collections.Generic;
using System.Threading;

using Android.Graphics;
using Android.OS;

using Soft.Crap.Caching;

// http://brett.duncavage.org/2014/02/in-memory-bitmap-caching-with.html
// https://github.com/rdio/tangoandcache

namespace Soft.Crap.Android.Caching
{
    public class AndroidBitmapDrawableCache
    {
        private readonly long _highWatermark;
        private readonly long _lowWatermark;
        private readonly long _gcThreshold;

        private bool _refillNeeded = true;
        private long _evictedSize;

        private readonly ReaderWriterLockSlim _cacheLock = new ReaderWriterLockSlim();

        private IDictionary<string, AndroidCachingBitmapDrawable> _displayedCache;
        private readonly PortableSizeAwareCache<string, AndroidCachingBitmapDrawable> _reusePool;        

        public AndroidBitmapDrawableCache
        (
            long highWatermark,
            long lowWatermark,
            long gcThreshold = 2 * 1024 * 1024
        )
        {
            _lowWatermark = lowWatermark;
            _highWatermark = highWatermark;

            _gcThreshold = gcThreshold;

            _displayedCache = new Dictionary<string, AndroidCachingBitmapDrawable>();

            _reusePool = new PortableSizeAwareCache<string, AndroidCachingBitmapDrawable>
            (
                highWatermark,
                lowWatermark,
                entryAdded : null,
                entryRemoved : (removedKey,
                                oldValue,
                                newValue,
                                isEvicted) =>
                {                   
                    ProcessRemoval(oldValue,
                                   isEvicted);
                }               
            );
        }        

        public void AddBitmapDrawableToCache
        (
            string bitmapKey,
            AndroidCachingBitmapDrawable bitmapDrawable
        )
        {            
            if ((bitmapDrawable == null) || (bitmapDrawable.Bitmap == null))
            {
                throw new ArgumentException("Attempt to add null value, refusing to cache.",
                                            nameof(bitmapDrawable));
            }

            _cacheLock.EnterWriteLock();

            try
            {
                if ((_displayedCache.ContainsKey(bitmapKey) == false) &&
                    (_reusePool.ContainsKey(bitmapKey) == false))
                {
                    _reusePool.Add(bitmapKey,
                                   bitmapDrawable);

                    bitmapDrawable.IsCached = true;
                    bitmapDrawable.CacheKey = bitmapKey;
                    bitmapDrawable.DisplayStarted = DisplayStarted;

                    UpdateByteUsage(bitmapDrawable.Bitmap,
                                    //decrement : false,
                                    eviction : false);                    
                }
            }
            finally
            {
                _cacheLock.ExitWriteLock();
            }
        }

        public AndroidCachingBitmapDrawable GetBitmapDrawableFromCache
        (
            string bitmapKey
        )
        {            
            AndroidCachingBitmapDrawable bitmapDrawable;

            bool isFound;

            _cacheLock.EnterReadLock();

            try
            {
                isFound = _displayedCache.TryGetValue(bitmapKey,
                                                      out bitmapDrawable);
                if (isFound)
                {
                    //total_cache_hits++; // cache hit
                }
                else
                {
                    isFound = _reusePool.TryGetValue(bitmapKey,
                                                     out bitmapDrawable);

                    // If key is found, its place in the LRU is refreshed

                    if (isFound)
                    {
                        //total_cache_hits++; // cache hit from reuse pool
                    }
                }
            }
            finally
            {
                _cacheLock.ExitReadLock();
            }            

            return bitmapDrawable;
        }

        public AndroidCachingBitmapDrawable GetReusableBitmapDrawable
        (
            int bitmapWidth,
            int bitmapHeight
        )
        {
            // Only attempt to get a bitmap for reuse if the reuse cache is full.
            // This prevents us from prematurely depleting the pool and allows
            // more cache hits, as the most recently added entries will have a high
            // likelihood of being accessed again so we don't want to steal those bytes
            // too soon.

            _cacheLock.EnterWriteLock(); // because of _refillNeeded

            try
            {
                if (_reusePool.CacheSizeInBytes < _lowWatermark && _refillNeeded)
                {
                    // Reuse pool is not full, refusing reuse request:
                    //total_reuse_misses++;

                    return null;
                }

                _refillNeeded = false;

                AndroidCachingBitmapDrawable reuseDrawable = null;

                if (_reusePool.Count > 0)
                {
                    foreach(string poolKey in _reusePool.Keys)
                    {
                        AndroidCachingBitmapDrawable poolDrawable = GetBitmapDrawableFromPool(poolKey,
                                                                                              updateLru : false);                        
                        // TODO: Implement check for KitKat and higher since
                        // bitmaps that are smaller than the requested size can be used.

                        if (Build.VERSION.SdkInt >= BuildVersionCodes.Kitkat)
                        {
                            // We can reuse a bitmap if the allocation to be reused is larger
                            // than the requested bitmap:

                            if ((poolDrawable.Bitmap.Width >= bitmapWidth) &&
                                (poolDrawable.Bitmap.Height >= bitmapHeight) &&
                                (poolDrawable.IsRetained == false))
                            {
                                reuseDrawable = poolDrawable;
                                break; // reuse hit, using larger allocation.
                            }
                        }
                        else if ((poolDrawable.Bitmap.Width == bitmapWidth) &&
                                 (poolDrawable.Bitmap.Height == bitmapHeight) &&
                                 (poolDrawable.Bitmap.IsMutable) &&
                                 (poolDrawable.IsRetained == false))
                        {
                            reuseDrawable = poolDrawable;
                            break; // reuse hit
                        }
                    }

                    if (reuseDrawable != null)
                    {
                        reuseDrawable.IsRetained = true;

                        UpdateByteUsage(reuseDrawable.Bitmap,
                                        //decrement : true,
                                        eviction : true);

                        // Cleanup the entry
                        reuseDrawable.DisplayStarted = null;
                        reuseDrawable.DisplayFinished = null;
                        reuseDrawable.IsCached = false;

                        _reusePool.Remove(reuseDrawable.CacheKey);

                        //total_reuse_hits++;
                    }
                }

                if (reuseDrawable == null)
                {
                    //total_reuse_misses++;

                    // Indicate that the pool may need to be refilled.
                    // There is little harm in setting this flag since it will be unset
                    // on the next reuse request if the threshold is
                    // _reusePool.CacheSizeInBytes >= _lowWatermark.

                    _refillNeeded = true;
                }

                return reuseDrawable;
            }
            finally
            {
                _cacheLock.ExitWriteLock();
            }
        }

        /*public ICollection<string> Keys
        {
            get
            {
                List<string> allKeys;

                _cacheLock.EnterReadLock();

                try
                {
                    ICollection<string> cacheKeys = _displayedCache.Keys;

                    allKeys = new List<string>(cacheKeys);

                    allKeys.AddRange(_reusePool.Keys);
                }
                finally
                {
                    _cacheLock.ExitReadLock();
                }

                return allKeys.AsReadOnly();
            }
        }*/

        /*public bool Remove
        (
            string bitmapKey
        )
        {
            bool isRemoved = false;

            AndroidCachingBitmapDrawable cachedDrawable;
            AndroidCachingBitmapDrawable reusedDrawable = null;

            _cacheLock.EnterWriteLock();

            try
            {
                if (_displayedCache.TryGetValue(bitmapKey,
                                                out cachedDrawable))
                {
                    isRemoved = _displayedCache.Remove(bitmapKey);
                }
                else if (_reusePool.TryGetValue(bitmapKey,
                                                out reusedDrawable))
                {
                    isRemoved = _reusePool.Remove(bitmapKey);
                }

                if (cachedDrawable != null)
                {
                    ProcessRemoval(cachedDrawable,
                                   isEvicted : true);
                }

                if (reusedDrawable != null)
                {
                    ProcessRemoval(reusedDrawable,
                                   isEvicted : true);
                }
            }
            finally
            {
                _cacheLock.ExitWriteLock();
            }

            return isRemoved;
        }*/

        /*public void Clear()
        {
            _cacheLock.EnterWriteLock();

            try
            {
                foreach(string displayedKey in _displayedCache.Keys)
                {
                    AndroidCachingBitmapDrawable displayedDrawable = _displayedCache[displayedKey];

                    if (displayedDrawable != null)
                    {
                        ProcessRemoval(displayedDrawable,
                                       isEvicted : true);
                    }
                }

                _displayedCache.Clear();

                foreach(string poolKey in _reusePool.Keys)
                {                    
                    AndroidCachingBitmapDrawable poolDrawable = GetBitmapDrawableFromPool(poolKey,
                                                                                          updateLru : true);
                    ProcessRemoval(poolDrawable,
                                   isEvicted : true);
                }

                _reusePool.Clear();
            }
            finally
            {
                _cacheLock.ExitWriteLock();
            }
        }*/

        /*public int Count
        {
            get
            {
                int totalCount;

                _cacheLock.EnterReadLock();

                try
                {
                    totalCount = _displayedCache.Count + _reusePool.Count;
                }
                finally
                {
                    _cacheLock.ExitReadLock();
                }

                return totalCount;
            }
        }*/

        private AndroidCachingBitmapDrawable GetBitmapDrawableFromPool
        (
            string poolKey,
            bool updateLru
        )
        {
            AndroidCachingBitmapDrawable poolDrawable;

            if (_reusePool.TryGetValue(poolKey,
                                       out poolDrawable,
                                       updateLru) == false)
            {
                throw new KeyNotFoundException(string.Format("Key not found: {0}",
                                                             poolKey));
            }

            return poolDrawable;
        }                

        private void DisplayStarted
        (
            AndroidCachingBitmapDrawable cachedDrawable
        )
        {            
            _cacheLock.EnterWriteLock();

            try
            {                
                if (_reusePool.ContainsKey(cachedDrawable.CacheKey))
                {
                    // promote reuse entry to displayed cache:

                    cachedDrawable.DisplayStarted = null;
                    cachedDrawable.DisplayFinished = DisplayFinished;

                    _reusePool.Remove(cachedDrawable.CacheKey);

                    _displayedCache.Add(cachedDrawable.CacheKey,
                                        cachedDrawable);
                }
            }
            finally
            {
                _cacheLock.ExitWriteLock();
            }
        }

        private void DisplayFinished
        (
            AndroidCachingBitmapDrawable cachedDrawable
        )
        {            
            _cacheLock.EnterWriteLock();

            try
            {
                if (_displayedCache.ContainsKey(cachedDrawable.CacheKey))
                {
                    // demote displayed entry to reuse pool

                    cachedDrawable.DisplayStarted = null;
                    cachedDrawable.DisplayFinished = DisplayStarted;

                    _displayedCache.Remove(cachedDrawable.CacheKey);

                    _reusePool.Add(cachedDrawable.CacheKey,
                                   cachedDrawable);
                }       
            }
            finally
            {
                _cacheLock.ExitWriteLock();
            }
        }

        private void ProcessRemoval
        (
            AndroidCachingBitmapDrawable bitmapDrawable,
            bool isEvicted
        )
        {
            if (_cacheLock.IsWriteLockHeld == false)
            {
                throw new SynchronizationLockException(nameof(_cacheLock));
            }

            // We only really care about evictions because we do direct Remove()als
            // all the time when promoting to the displayed cache. Only when the
            // entry has been evicted is it truly not longer being held by us.

            if (isEvicted)
            {
                UpdateByteUsage(bitmapDrawable.Bitmap,
                                //decrement: true,
                                eviction: true);

                bitmapDrawable.DisplayStarted = null;
                bitmapDrawable.DisplayFinished = null;
                bitmapDrawable.IsCached = false;
            }
        }

        private void UpdateByteUsage
        (
            Bitmap bitmap,
            //bool decrement,
            bool eviction
        )
        {
            if (_cacheLock.IsWriteLockHeld == false)
            {
                throw new SynchronizationLockException(nameof(_cacheLock));
            }

            int byteCount = bitmap.RowBytes * bitmap.Height;
            //_cacheSize += byteCount * (decrement ? -1 : 1);

            if (eviction)
            {
                _evictedSize += byteCount;

                // Kick the gc if we've accrued more than our desired threshold.
                // TODO: Implement high/low watermarks to prevent thrashing

                if (_evictedSize > _gcThreshold)
                {
                    // Memory usage exceeds threshold, invoking GC.Collect:

                    GC.Collect();

                    _evictedSize = 0;
                }
            }
        }
    }
}