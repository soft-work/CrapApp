using System;
using System.Threading;

using Android.Content.Res;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;

using Soft.Crap.Caching;

// http://brett.duncavage.org/2014/02/in-memory-bitmap-caching-with.html
// https://github.com/rdio/tangoandcache

namespace Soft.Crap.Android.Caching
{        
    public class AndroidCachingBitmapDrawable : BitmapDrawable,
                                                PortableSizeAwareEntry
    {
        private static readonly bool _reuseBitmap 
            = (Build.VERSION.SdkInt >= BuildVersionCodes.Honeycomb);

        private readonly ReaderWriterLockSlim _stateLock = new ReaderWriterLockSlim();
        private int _cacheCount;
        private int _displayCount;
        private int _retainCount;
        private bool _isDisposed;        

        public AndroidCachingBitmapDrawable
        (
            Resources resources,
            Bitmap bitmap
        )
        : base(resources,
               bitmap) { }                

        long PortableSizeAwareEntry.SizeInBytes
        {
            get
            {
                long sizeInBytes;

                _stateLock.EnterReadLock();

                try
                {
                    sizeInBytes = (HasValidBitmap())
                                ? Bitmap.Height * Bitmap.RowBytes
                                : 0;                    
                }
                finally
                {
                    _stateLock.ExitReadLock();
                }

                return sizeInBytes;
            }
        }

        public string CacheKey { set; get; }

        public Action<AndroidCachingBitmapDrawable> DisplayStarted { set; private get; }
        public Action<AndroidCachingBitmapDrawable> DisplayFinished { set; private get; }

        public bool IsDisplayed
        {
            set
            {
                Action<AndroidCachingBitmapDrawable> displayChanged = null;

                _stateLock.EnterWriteLock();

                try
                {
                    if (value)
                    {
                        if (HasValidBitmap() == false)
                        {
                            return;

                            /*throw new InvalidOperationException
                            (
                                "Cannot re-display this drawable, its resources have been disposed."
                            );*/
                        }

                        _displayCount++;

                        if (_displayCount == 1)
                        {
                            displayChanged = DisplayStarted;
                        }
                    }
                    else
                    {
                        _displayCount--;
                    }

                    if (_displayCount <= 0)
                    {
                        displayChanged = DisplayFinished;
                    }
                }
                finally
                {
                    _stateLock.ExitWriteLock();
                }

                displayChanged?.Invoke(this);

                CheckState();
            }
        }

        public bool IsCached
        {
            set
            {
                _stateLock.EnterWriteLock();

                try
                {
                    if (value)
                    {
                        _cacheCount++;
                    }
                    else
                    {
                        _cacheCount--;
                    }                    
                }
                finally
                {
                    _stateLock.ExitWriteLock();
                }

                CheckState();
            }
        }

        public bool IsRetained
        {
            set
            {
                _stateLock.EnterWriteLock();

                try
                {
                    if (value)
                    {
                        _retainCount++;
                    }
                    else
                    {
                        _retainCount--;
                    }                    
                }
                finally
                {
                    _stateLock.ExitWriteLock();
                }

                CheckState();
            }

            get
            {
                bool isRetained; 

                _stateLock.EnterReadLock();

                try
                {
                    isRetained = _retainCount > 0;
                }
                finally
                {
                    _stateLock.ExitReadLock();
                }

                return isRetained;
            }
        }

        private bool HasValidBitmap()
        {
            if ((_stateLock.IsReadLockHeld || _stateLock. IsWriteLockHeld) == false)
            {
                throw new SynchronizationLockException(nameof(_stateLock));
            }

            bool hasValidBitmap = ((Bitmap != null) &&
                                  (_isDisposed == false) &&
                                  (Bitmap.IsRecycled == false));

            return hasValidBitmap;            
        }        

        private void CheckState()
        {
            _stateLock.EnterReadLock();

            try
            {
                bool shouldFreeResources = ((_cacheCount <= 0) &&
                                            (_displayCount <= 0) &&
                                            (_retainCount <= 0) &&
                                            HasValidBitmap());
                if (shouldFreeResources)
                {
                    if (_reuseBitmap)
                    {
                        Bitmap.Dispose();
                        _isDisposed = true;
                    }
                    else
                    {
                        Bitmap.Recycle();
                    }
                }
            }
            finally
            {
                _stateLock.ExitReadLock();
            }
        }        
    }
}