using System.Collections.Concurrent;
using System.Windows.Media.Imaging;

namespace FindAncestor.Roc
{
    public class RecordingEngine
    {
        private readonly FFmpegNvencRecorder _recorder = new();

        private BlockingCollection<BitmapSource> _queue = new(300); // 🔥 戻す
        private Thread? _thread;
        private volatile bool _isRecording;

        private string? _outputPath;

        public event Action<string>? RecordingCompleted;

        public void Start(string path, int w, int h)
        {
            Stop();

            _outputPath = path;

            _queue = new BlockingCollection<BitmapSource>(300); // 🔥 余裕持たせる

            _recorder.Start(path, w, h, 60, null);

            _isRecording = true;

            _thread = new Thread(EncodeLoop)
            {
                IsBackground = true
            };
            _thread.Start();
        }

        public void EnqueueFrame(BitmapSource bmp)
        {
            if (!_isRecording) return;

            try
            {
                // 🔥 BLOCKING（これが超重要）
                _queue.Add(bmp);
            }
            catch { }
        }

        private void EncodeLoop()
        {
            try
            {
                foreach (var bmp in _queue.GetConsumingEnumerable())
                {
                    _recorder.AddFrameAsync(bmp);
                }
            }
            catch { }
        }

        public async Task StopAsync()
        {
            if (!_isRecording) return;

            _isRecording = false;

            try { _queue.CompleteAdding(); } catch { }

            // 🔥 完全排出待ち
            try { await Task.Run(() => _thread?.Join()); } catch { }

            await _recorder.StopAsync();

            if (_outputPath != null)
                RecordingCompleted?.Invoke(_outputPath);
        }

        public void Stop()
        {
            StopAsync().GetAwaiter().GetResult();
        }
    }
}