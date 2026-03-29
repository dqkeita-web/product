using System.Collections.Concurrent;

namespace FindAncestor.Voice.OCR
{
    public class OcrCacheService
    {
        private readonly ConcurrentDictionary<string, string> _cache = new();

        public bool TryGet(string key, out string value)
            => _cache.TryGetValue(key, out value);

        public void Set(string key, string value)
            => _cache[key] = value;
    }
}