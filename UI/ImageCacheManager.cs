using System;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using SnipShottyBoard.Data;

namespace SnipShottyBoard.UI
{
    /// <summary>
    /// LRU image cache with dual eviction: count-based (100 items) and size-based (100MB).
    /// All access is on the WPF dispatcher thread — no locking needed.
    /// </summary>
    public class ImageCacheManager
    {
        private static ImageCacheManager? _instance;
        public static ImageCacheManager Instance => _instance ??= new ImageCacheManager();

        private readonly Dictionary<string, LinkedListNode<CacheEntry>> _index;
        private readonly LinkedList<CacheEntry> _lruList;
        private readonly int _maxItems;
        private readonly long _maxBytes;
        private long _currentBytes;

        private ImageCacheManager()
        {
            _maxItems = AppConstants.MaxCachedImages;
            _maxBytes = AppConstants.MaxImageCacheBytes;
            _index = new Dictionary<string, LinkedListNode<CacheEntry>>(_maxItems);
            _lruList = new LinkedList<CacheEntry>();
            _currentBytes = 0;
        }

        /// <summary>
        /// Returns the cached BitmapImage for the given path, or null if not cached.
        /// Promotes the entry to most-recently-used on a cache hit.
        /// </summary>
        public BitmapImage? GetFromCache(string path)
        {
            if (!_index.TryGetValue(path, out var node))
                return null;

            // Move to front = most recently used
            // Defensive: node may have been removed from list by concurrent RemoveAllForPath
            try { _lruList.Remove(node); } catch { /* node already removed — safe */ }

            // If node was orphaned, return null (caller will re-decode)
            if (node.List == null)
            {
                _index.Remove(path);
                return null;
            }

            _lruList.AddFirst(node.Value);
            _index[path] = _lruList.First!;

            return node.Value.Bitmap;
        }

        /// <summary>
        /// Stores a BitmapImage in the cache keyed by path.
        /// Evicts LRU entries until both count and size limits are satisfied.
        /// If the path is already cached, the call is ignored.
        /// </summary>
        public void AddToCache(string path, BitmapImage bitmap)
        {
            if (_index.ContainsKey(path))
                return;

            long entryBytes = EstimateBitmapBytes(bitmap);

            // Evict until both limits are satisfied
            while ((_index.Count >= _maxItems || _currentBytes + entryBytes > _maxBytes) && _lruList.Last != null)
                EvictLeastRecentlyUsed();

            var entry = new CacheEntry(path, bitmap, entryBytes);
            _lruList.AddFirst(entry);
            _index[path] = _lruList.First!;
            _currentBytes += entryBytes;
        }

        /// <summary>
        /// Removes an entry from the cache (e.g. when a file is deleted).
        /// </summary>
        public void RemoveFromCache(string path)
        {
            if (!_index.TryGetValue(path, out var node))
                return;

            _currentBytes -= node.Value.Bytes;

            // Defensive: node may have been orphaned by concurrent GetFromCache
            try { _lruList.Remove(node); } catch { /* already removed — safe */ }

            _index.Remove(path);
        }

        /// <summary>
        /// Removes all cache entries for a given file path, including any
        /// prefixed variants (e.g. "path:full" for full-res viewer cache).
        /// </summary>
        public void RemoveAllForPath(string path)
        {
            // Remove the base key
            RemoveFromCache(path);

            // Remove any prefixed variants (e.g. path:full)
            var keysToRemove = _index.Keys.Where(k => k.StartsWith(path)).ToList();
            foreach (var key in keysToRemove)
                RemoveFromCache(key);
        }

        /// <summary>
        /// Clears all entries. Call on shutdown to release memory.
        /// </summary>
        public void Clear()
        {
            _lruList.Clear();
            _index.Clear();
            _currentBytes = 0;
        }

        private static long EstimateBitmapBytes(BitmapImage bitmap)
        {
            // Approximate: decoded pixel count × 4 bytes (BGRA) × 2 (back buffer)
            return (long)bitmap.PixelWidth * bitmap.PixelHeight * 4 * 2;
        }

        private void EvictLeastRecentlyUsed()
        {
            var oldest = _lruList.Last;
            if (oldest == null) return;

            _currentBytes -= oldest.Value.Bytes;
            _lruList.RemoveLast();
            _index.Remove(oldest.Value.Path);
        }

        private sealed class CacheEntry
        {
            public string Path { get; }
            public BitmapImage Bitmap { get; }
            public long Bytes { get; }

            public CacheEntry(string path, BitmapImage bitmap, long bytes)
            {
                Path = path;
                Bitmap = bitmap;
                Bytes = bytes;
            }
        }
    }
}
