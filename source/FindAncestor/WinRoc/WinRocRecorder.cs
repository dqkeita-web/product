using FindAncestor.Roc;
using FindAncestor.ErrorDialog;
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
            // 🔴 領域チェック
            if (!region.IsValid || region.Width <= 0 || region.Height <= 0)
            {
                ErrorDialogHelper.Show($"❌ 録画領域が不正\nW:{region.Width} H:{region.Height}");
                return Task.CompletedTask;
            }

            // 🔴 偶数補正（超重要）
            int width = region.Width / 2 * 2;
            int height = region.Height / 2 * 2;

            if (width <= 0 || height <= 0)
            {
                ErrorDialogHelper.Show($"❌ 偶数補正後サイズが0\nW:{width} H:{height}");
                return Task.CompletedTask;
            }

            _region = new WinRocRegion
            {
                X = region.X,
                Y = region.Y,
                Width = width,
                Height = height
            };

            try
            {
                _engine.Start(path, width, height);
            }
            catch (Exception ex)
            {
                ErrorDialogHelper.Show("❌ RecordingEngine.Start失敗\n" + ex.Message);
                return Task.CompletedTask;
            }

            try
            {
                _duplicator = new DxgiDuplicator();
            }
            catch (Exception ex)
            {
                ErrorDialogHelper.Show("❌ DxgiDuplicator初期化失敗\n" + ex.Message);
                return Task.CompletedTask;
            }

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
            int failCount = 0;

            while (_state.IsRecording && !_state.IsStopping)
            {
                DxgiFrame? frame = null;

                try
                {
                    frame = _duplicator?.Capture(_region);
                }
                catch (Exception ex)
                {
                    ErrorDialogHelper.Show("❌ Capture例外\n" + ex.Message);
                    break;
                }

                if (frame == null)
                {
                    failCount++;

                    if (failCount % 100 == 0)
                    {
                        Debug.WriteLine("⚠ フレーム取得失敗継続");

                        ErrorDialogHelper.Show(
                            $"⚠ フレーム取得失敗\n{failCount}回連続\n" +
                            $"領域 W:{_region.Width} H:{_region.Height}");
                    }

                    Thread.Sleep(1);
                    continue;
                }

                failCount = 0;

                try
                {
                    var bmp = BitmapSource.Create(
                        frame.Width,
                        frame.Height,
                        96,
                        96,
                        PixelFormats.Bgra32,
                        null,
                        frame.Buffer,
                        frame.Stride);

                    bmp.Freeze();

                    _engine.EnqueueFrame(bmp);
                }
                catch (Exception ex)
                {
                    ErrorDialogHelper.Show("❌ Enqueue失敗\n" + ex.Message);
                    Thread.Sleep(1);
                    continue;
                }
            }
        }

        // =========================
        // 停止
        // =========================
        public async Task StopAsync()
        {
            if (!_state.IsRecording || _state.IsStopping)
                return;

            Debug.WriteLine("WinRoc: 停止");

            _state.IsStopping = true;

            try
            {
                _thread?.Join();
            }
            catch (Exception ex)
            {
                ErrorDialogHelper.Show("❌ Thread.Join失敗\n" + ex.Message);
            }

            _state.IsRecording = false;

            try
            {
                _duplicator?.Dispose();
            }
            catch (Exception ex)
            {
                ErrorDialogHelper.Show("❌ Duplicator Dispose失敗\n" + ex.Message);
            }

            try
            {
                await _engine.StopAsync();
            }
            catch (Exception ex)
            {
                ErrorDialogHelper.Show("❌ Engine Stop失敗\n" + ex.Message);
            }
        }
    }
}