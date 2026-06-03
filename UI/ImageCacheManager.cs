using System;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using SnipShottyBoard.Data;

namespace SnipShottyBoard.UI
{
    /// <summary>
    /// LRU image cache with two independent pools:
    ///   Thumbnail pool — 30MB / 60 items (AppConstants). Small, many, reused constantly.
    ///   Full-res pool  — 5 items max, no byte cap. Large, infrequently reused; viewer
    ///                    close handles explicit eviction via RemoveFromCache(path + ":full").
    /// Keys ending with ":full" are routed to the full-res pool automatically.
    /// All access is on the WPF dispatcher thread — no locking needed.
    /// </summary>
    public class ImageCacheManager
    {
        private static ImageCacheManager? _instance;
        public static ImageCacheManager Instance => _instance ??= new ImageCacheManager();

        // Thumbnail pool
        private readonly Dictionary<string, LinkedListNode<CacheEntry>> _index;
        private readonly LinkedList<CacheEntry> _lruList;
        private readonly int _maxItems;
        private readonly long _maxBytes;
        private long _currentBytes;

        // Full-res pool
        private readonly Dictionary<string, LinkedListNode<CacheEntry>> _fullResIndex;
        private readonly LinkedList<CacheEntry> _fullResLruList;
        private long _fullResCurrentBytes;
        private const int MaxFullResItems = 5;

        private ImageCacheManager()
        {
            _maxItems = AppConstants.MaxCachedImages;
            _maxBytes = AppConstants.MaxImageCacheBytes;
            _index = new Dictionary<string, LinkedListNode<CacheEntry>>(_maxItems);
            _lruList = new LinkedList<CacheEntry>();
            _currentBytes = 0;

            _fullResIndex = new Dictionary<string, LinkedListNode<CacheEntry>>(MaxFullResItems);
            _fullResLruList = new LinkedList<CacheEntry>();
            _fullResCurrentBytes = 0;
        }

        /// <summary>
        /// Returns the cached BitmapImage for the given path, or null if not cached.
        /// Promotes the entry to most-recently-used on a cache hit.
        /// Keys ending with ":full" are served from the full-res pool.
        /// </summary>
        public BitmapImage? GetFromCache(string path)
        {
            if (path.EndsWith(":full"))
                return GetFromFullResPool(path);

            if (!_index.TryGetValue(path, out var node))
                return null;

            try { _lruList.Remove(node); } catch { /* node already removed — safe */ }

            if (node.List == null)
            {
                _index.Remove(path);
                return null;
            }

            _lruList.AddFirst(node.Value);
            _index[path] = _lruList.First!;
            return node.Value.Bitmap;
        }

        private BitmapImage? GetFromFullResPool(string path)
        {
            if (!_fullResIndex.TryGetValue(path, out var node))
                return null;

            try { _fullResLruList.Remove(node); } catch { /* node already removed — safe */ }

            if (node.List == null)
            {
                _fullResIndex.Remove(path);
                return null;
            }

            _fullResLruList.AddFirst(node.Value);
            _fullResIndex[path] = _fullResLruList.First!;
            return node.Value.Bitmap;
        }

        /// <summary>
        /// Stores a BitmapImage in the cache keyed by path.
        /// Keys ending with ":full" go into the full-res pool (evicted by count, max 5).
        /// All other keys go into the thumbnail pool (evicted by count + bytes).
        /// If the path is already cached, the call is ignored.
        /// </summary>
        public void AddToCache(string path, BitmapImage bitmap)
        {
            if (path.EndsWith(":full"))
            {
                AddToFullResPool(path, bitmap);
                return;
            }

            if (_index.ContainsKey(path))
                return;

            long entryBytes = EstimateBitmapBytes(bitmap);

            while ((_index.Count >= _maxItems || _currentBytes + entryBytes > _maxBytes) && _lruList.Last != null)
                EvictLeastRecentlyUsed();

            var entry = new CacheEntry(path, bitmap, entryBytes);
            _lruList.AddFirst(entry);
            _index[path] = _lruList.First!;
            _currentBytes += entryBytes;
        }

        private void AddToFullResPool(string path, BitmapImage bitmap)
        {
            if (_fullResIndex.ContainsKey(path))
                return;

            long entryBytes = EstimateBitmapBytes(bitmap);

            // Count-only eviction — viewer close handles explicit cleanup via RemoveFromCache
            while (_fullResIndex.Count >= MaxFullResItems && _fullResLruList.Last != null)
                EvictLeastRecentlyUsedFullRes();

            var entry = new CacheEntry(path, bitmap, entryBytes);
            _fullResLruList.AddFirst(entry);
            _fullResIndex[path] = _fullResLruList.First!;
            _fullResCurrentBytes += entryBytes;
        }

        /// <summary>
        /// Removes an entry from the cache (e.g. when a file is deleted or viewer closes).
        /// Keys ending with ":full" are removed from the full-res pool.
        /// </summary>
        public void RemoveFromCache(string path)
        {
            if (path.EndsWith(":full"))
            {
                RemoveFromFullResPool(path);
                return;
            }

            if (!_index.TryGetValue(path, out var node))
                return;

            _currentBytes -= node.Value.Bytes;
            try { _lruList.Remove(node); } catch { /* already removed — safe */ }
            _index.Remove(path);
        }

        private void RemoveFromFullResPool(string path)
        {
            if (!_fullResIndex.TryGetValue(path, out var node))
                return;

            _fullResCurrentBytes -= node.Value.Bytes;
            try { _fullResLruList.Remove(node); } catch { /* already removed — safe */ }
            _fullResIndex.Remove(path);
        }

        /// <summary>
        /// Removes all cache entries for a given file path across both pools,
        /// including any suffixed variants (e.g. "path:full").
        /// </summary>
        public void RemoveAllForPath(string path)
        {
            RemoveFromCache(path);

            // Remove thumbnail pool variants
            var thumbKeys = _index.Keys.Where(k => k.StartsWith(path)).ToList();
            foreach (var key in thumbKeys)
                RemoveFromCache(key);

            // Remove full-res pool variants
            var fullResKeys = _fullResIndex.Keys.Where(k => k.StartsWith(path)).ToList();
            foreach (var key in fullResKeys)
                RemoveFromFullResPool(key);
        }

        /// <summary>
        /// Clears all entries in both pools. Call on shutdown to release memory.
        /// </summary>
        public void Clear()
        {
            _lruList.Clear();
            _index.Clear();
            _currentBytes = 0;

            _fullResLruList.Clear();
            _fullResIndex.Clear();
            _fullResCurrentBytes = 0;
        }

        private static long EstimateBitmapBytes(BitmapImage bitmap)
        {
            // WPF allocates decoded pixel buffer + milcore compositor copy + DPI scaling copy (~×3)
            return (long)bitmap.PixelWidth * bitmap.PixelHeight * 4 * 3;
        }

        private void EvictLeastRecentlyUsed()
        {
            var oldest = _lruList.Last;
            if (oldest == null) return;

            _currentBytes -= oldest.Value.Bytes;
            _lruList.RemoveLast();
            _index.Remove(oldest.Value.Path);

            // Compact LOH after evicting large bitmaps — prevents committed RAM staying high
            System.Runtime.GCSettings.LargeObjectHeapCompactionMode =
                System.Runtime.GCLargeObjectHeapCompactionMode.CompactOnce;
        }

        private void EvictLeastRecentlyUsedFullRes()
        {
            var oldest = _fullResLruList.Last;
            if (oldest == null) return;

            _fullResCurrentBytes -= oldest.Value.Bytes;
            _fullResLruList.RemoveLast();
            _fullResIndex.Remove(oldest.Value.Path);

            System.Runtime.GCSettings.LargeObjectHeapCompactionMode =
                System.Runtime.GCLargeObjectHeapCompactionMode.CompactOnce;
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
