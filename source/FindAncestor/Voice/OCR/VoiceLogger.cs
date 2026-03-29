using Microsoft.Extensions.Logging;

namespace FindAncestor.Voice.OCR
{
    public class VoiceLogger
    {
        private readonly ILogger _logger;

        public VoiceLogger(ILogger logger)
        {
            _logger = logger;
        }

        public void LogOcr(long ms)
            => _logger.LogInformation($"OCR: {ms}ms");
    }
}