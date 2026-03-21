namespace FindAncestor.Roc
{
    using System.Collections.Concurrent;
    using System.Windows.Media.Imaging;

    public class RecordingEngine
    {
        private readonly FFmpegNvencRecorder _recorder = new();

        private BlockingCollection<byte[]> _queue = new(300);
        private Thread? _thread;
        private volatile bool _isRecording;

        private int _width;
        private int _height;
        private int _frameSize;

        public event Action<string>? RecordingCompleted;
        private string? _outputPath;

        public void Start(string path, int w, int h)
        {
            Stop();

            _width = w;
            _height = h;
            _frameSize = w * h * 4;

            _outputPath = path;

            _queue = new BlockingCollection<byte[]>(300);

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
                int stride = _width * 4;

                var buffer = new byte[_frameSize];

                // 🔥 stride固定でコピー（超重要）
                bmp.CopyPixels(buffer, stride, 0);

                _queue.Add(buffer);
            }
            catch { }
        }

        private void EncodeLoop()
        {
            try
            {
                foreach (var buffer in _queue.GetConsumingEnumerable())
                {
                    _recorder.WriteRaw(buffer);
                }
            }
            catch { }
        }

        public async Task StopAsync()
        {
            if (!_isRecording) return;

            _isRecording = false;

            _queue.CompleteAdding();

            await Task.Run(() => _thread?.Join());

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