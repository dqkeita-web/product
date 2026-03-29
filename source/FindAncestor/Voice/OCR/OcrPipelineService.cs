using System.Threading;
using System.Threading.Tasks;

namespace FindAncestor.Voice.OCR
{
    public class OcrPipelineService
    {
        private readonly IOcrService _ocr;
        private readonly OcrCacheService _cache;

        public OcrPipelineService(IOcrService ocr, OcrCacheService cache)
        {
            _ocr = ocr;
            _cache = cache;
        }

        public async Task<string> ExtractAsync(string path, CancellationToken ct)
        {
            if (_cache.TryGet(path, out var cached))
                return cached;

            var text = await _ocr.ReadAsync(path, ct);

            _cache.Set(path, text);

            return text;
        }
    }
}