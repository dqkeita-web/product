using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using FindAncestor.ErrorDialog;

namespace FindAncestor.Roc
{
    public class RecordingEngine
    {
        private readonly FFmpegNvencRecorder _recorder = new();

        private BlockingCollection<byte[]> _queue = new(300);
        private Thread? _thread;
        private volatile bool _isRecording;

        private int _width;
        private int _height;
        private int _frameSize;

        private string? _outputPath;

        public event Action<string>? RecordingCompleted;

        public void Start(string path, int w, int h)
        {
            try
            {
                Stop();

                if (w <= 0 || h <= 0)
                {
                    ErrorDialogHelper.Show($"❌ RecordingEngine.Start 不正サイズ\nW:{w} H:{h}");
                    return;
                }

                if (w % 2 != 0 || h % 2 != 0)
                {
                    ErrorDialogHelper.Show($"⚠ 偶数でないサイズ\nW:{w} H:{h}");
                }

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
            catch (Exception ex)
            {
                ErrorDialogHelper.Show("❌ RecordingEngine.Start例外\n" + ex);
            }
        }

        public void EnqueueFrame(BitmapSource bmp)
        {
            if (!_isRecording)
                return;

            try
            {
                int stride = _width * 4;

                var buffer = new byte[_frameSize];

                bmp.CopyPixels(buffer, stride, 0);

                if (bmp.PixelWidth <= 0 || bmp.PixelHeight <= 0)
                {
                    ErrorDialogService.Show("EnqueueFrame: サイズ0");
                    return;
                }

                _queue.Add(buffer);
            }
            catch (Exception ex)
            {
                ErrorDialogService.Show(ex);
            }
        }

        private void EncodeLoop()
        {
            try
            {
                foreach (var buffer in _queue.GetConsumingEnumerable())
                {
                    try
                    {
                        _recorder.WriteRaw(buffer);
                    }
                    catch (Exception ex)
                    {
                        ErrorDialogHelper.Show("❌ WriteRaw例外\n" + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorDialogHelper.Show("❌ EncodeLoop例外\n" + ex);
            }
        }

        public async Task StopAsync()
        {
            try
            {
                if (!_isRecording)
                    return;

                _isRecording = false;

                _queue.CompleteAdding();

                await Task.Run(() => _thread?.Join());

                await _recorder.StopAsync();

                if (_outputPath != null)
                    RecordingCompleted?.Invoke(_outputPath);
            }
            catch (Exception ex)
            {
                ErrorDialogHelper.Show("❌ StopAsync例外\n" + ex);
            }
        }

        public void Stop()
        {
            try
            {
                StopAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                ErrorDialogHelper.Show("❌ Stop例外\n" + ex);
            }
        }
    }
}