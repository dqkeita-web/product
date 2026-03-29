using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Tesseract;

namespace FindAncestor.Voice.OCR
{
    public class OcrService : IOcrService
    {
        private readonly string _tessPath;

        public OcrService(string tessPath = "tessdata")
        {
            _tessPath = tessPath;
        }

        public async Task<string> ReadAsync(string path, CancellationToken ct)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException(path);

            return await Task.Run(() =>
            {
                using var engine = new TesseractEngine(_tessPath, "jpn+eng");
                using var img = Pix.LoadFromFile(path);
                using var page = engine.Process(img);
                return page.GetText()?.Trim() ?? "";
            }, ct);
        }
    }
}