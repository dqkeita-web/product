using FindAncestor.Roc;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FindAncestor.WinRoc
{
    public class WinRocRecorder
    {
        private readonly RecordingEngine _engine;
        private readonly WinRocState _state = new();

        private DxgiDuplicator? _duplicator;
        private Thread? _thread;
        private WinRocRegion _region;

        public WinRocRecorder(RecordingEngine engine)
        {
            _engine = engine;
        }

        // =========================
        // 録画開始
        // =========================
        public Task StartRecordingAsync(string path, WinRocRegion region)
        {
            _region = region;

            _engine.Start(path, region.Width, region.Height);

            _duplicator = new DxgiDuplicator();

            _state.IsRecording = true;
            _state.IsStopping = false;

            _thread = new Thread(CaptureLoop)
            {
                IsBackground = true
            };
            _thread.Start();

            return Task.CompletedTask;
        }

        // =========================
        // キャプチャループ
        // =========================
        private void CaptureLoop()
        {
            while (_state.IsRecording && !_state.IsStopping)
            {
                var frame = _duplicator?.Capture(_region); // ← これが正しい
                if (frame == null) continue;

                try
                {
                    _engine.EnqueueFrame(
                        BitmapSource.Create(
                            frame.Width,
                            frame.Height,
                            96, 96,
                            PixelFormats.Bgra32,
                            null,
                            frame.Buffer,
                            frame.Stride));
                }
                catch
                {
                    break;
                }
            }
        }        // =========================
        // 停止
        // =========================
        public async Task StopAsync()
        {
            if (!_state.IsRecording || _state.IsStopping)
                return;

            Debug.WriteLine("WinRoc: 停止");

            _state.IsStopping = true;

            _thread?.Join();

            _state.IsRecording = false;

            _duplicator?.Dispose();

            await _engine.StopAsync();
        }

        private byte[] CropBuffer(DxgiFrame frame, WinRocRegion r, out int stride)
        {
            int bytesPerPixel = 4;
            stride = r.Width * bytesPerPixel;

            byte[] buffer = new byte[stride * r.Height];

            for (int y = 0; y < r.Height; y++)
            {
                int srcIndex = ((r.Y + y) * frame.Stride) + (r.X * bytesPerPixel);
                int dstIndex = y * stride;

                Buffer.BlockCopy(frame.Buffer, srcIndex, buffer, dstIndex, stride);
            }

            return buffer;
        }
    }
}