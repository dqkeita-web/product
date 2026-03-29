using System.Threading;
using System.Threading.Tasks;

namespace FindAncestor.Voice.OCR
{
    public interface IOcrService
    {
        Task<string> ReadAsync(string path, CancellationToken ct);
    }
}