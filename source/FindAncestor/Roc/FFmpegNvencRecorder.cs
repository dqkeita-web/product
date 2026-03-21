using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FindAncestor.Roc
{
    public class FFmpegNvencRecorder
    {
        private Process? _ffmpeg;
        private volatile bool _isStopping;

        private readonly string ffmpegPath = @"C:\Tools\ffmpeg\bin\ffmpeg.exe";

        public void Start(string outputPath, int width, int height, int fps, string? audioPath)
        {
            _isStopping = false;

            string args =
                $"-y -f rawvideo -pix_fmt bgra -s {width}x{height} -i - " +
                $"-r {fps} " +
                "-c:v libx264 -preset ultrafast -crf 18 " +
                "-pix_fmt yuv420p " +
                $"{outputPath}";

            Debug.WriteLine("FFmpeg Args: " + args);
            _ffmpeg = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                },
                EnableRaisingEvents = true
            };

            // プロセス終了通知
            _ffmpeg.Exited += (s, e) =>
            {
                Debug.WriteLine("FFmpeg exited");
            };

            // 標準エラーのリアルタイム読み取り
            _ffmpeg.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    Debug.WriteLine("[FFmpeg stderr] " + e.Data);
            };

            _ffmpeg.Start();
            _ffmpeg.BeginErrorReadLine(); // これが必須
            _ffmpeg = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            _ffmpeg.Start();

            // 🔴 FFmpegログ出力（超重要）
            _ffmpeg.ErrorDataReceived += (s, e) =>
            {
                if (e.Data != null)
                    Debug.WriteLine("FFMPEG: " + e.Data);
            };
            _ffmpeg.BeginErrorReadLine();
        }

        public async Task AddFrameAsync(BitmapSource bmp)
        {
            if (_isStopping || _ffmpeg == null || _ffmpeg.HasExited)
                return;

            try
            {
                // 🔴 BGRAに強制変換
                var converted = new FormatConvertedBitmap();
                converted.BeginInit();
                converted.Source = bmp;
                converted.DestinationFormat = PixelFormats.Bgra32;
                converted.EndInit();

                int stride = converted.PixelWidth * 4;
                byte[] pixels = new byte[stride * converted.PixelHeight];

                converted.CopyPixels(pixels, stride, 0);

                // 🔴 非同期書き込み（詰まり対策）
                await _ffmpeg.StandardInput.BaseStream.WriteAsync(pixels, 0, pixels.Length);

                Debug.WriteLine($"Frame written: {pixels.Length} bytes");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("AddFrame ERROR: " + ex.ToString());
            }
        }

        public async Task StopAsync()
        {
            if (_isStopping) return;
            _isStopping = true;

            try
            {
                if (_ffmpeg == null) return;

                Debug.WriteLine("Stopping FFmpeg...");

                await _ffmpeg.StandardInput.BaseStream.FlushAsync();
                _ffmpeg.StandardInput.Close();

                await Task.Run(() => _ffmpeg.WaitForExit());

                Debug.WriteLine("FFmpeg exited.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Stop ERROR: " + ex.ToString());
            }

            try { _ffmpeg?.Dispose(); } catch { }

            _ffmpeg = null;
        }
    }
}