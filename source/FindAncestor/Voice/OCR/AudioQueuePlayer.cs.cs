using System.Threading.Channels;
using System.Threading;
using System.Threading.Tasks;

namespace FindAncestor.Voice.OCR
{
    public class AudioQueuePlayer
    {
        private readonly Channel<byte[]> _queue = Channel.CreateBounded<byte[]>(3);
        private readonly VoicePlaybackService _player;

        public AudioQueuePlayer(VoicePlaybackService player)
        {
            _player = player;
        }

        public async Task EnqueueAsync(byte[] wav, CancellationToken ct)
        {
            await _queue.Writer.WriteAsync(wav, ct);
        }

        public async Task StartAsync(CancellationToken ct)
        {
            await foreach (var wav in _queue.Reader.ReadAllAsync(ct))
            {
                await _player.PlayAsync(wav, ct);
            }
        }
    }
}