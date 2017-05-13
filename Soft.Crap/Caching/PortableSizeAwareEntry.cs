using System;

// http://brett.duncavage.org/2014/02/in-memory-bitmap-caching-with.html
// https://github.com/rdio/tangoandcache

namespace Soft.Crap.Caching
{
    public interface PortableSizeAwareEntry
    {
        long SizeInBytes { get; }
    }
}