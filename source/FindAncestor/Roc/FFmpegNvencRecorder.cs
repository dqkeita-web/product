using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace FindAncestor.Roc
{
    public class FFmpegNvencRecorder
    {
        private Process? _ffmpeg;
        private volatile bool _isStopping;

        private readonly string ffmpegPath = @"C:\Tools\ffmpeg\bin\ffmpeg.exe";

        // 🔥 追加：バッファ再利用
        private byte[]? _buffer;

        public void Start(string outputPath, int width, int height, int fps, string? audioPath)
        {
            _isStopping = false;

            string args =
                $"-y -f rawvideo -pix_fmt bgra -s {width}x{height} -r {fps} -i - " +
                "-c:v h264_nvenc -preset p5 -rc vbr -cq 19 " +
                "-pix_fmt yuv420p " +
                $"{outputPath}";

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

            _ffmpeg.ErrorDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    Debug.WriteLine("FFmpeg: " + e.Data);
            };
            _ffmpeg.BeginErrorReadLine();
        }

        public void AddFrame(BitmapSource bmp)
        {
            if (_isStopping || _ffmpeg == null || _ffmpeg.HasExited)
                return;

            try
            {
                int stride = bmp.PixelWidth * 4;
                int size = stride * bmp.PixelHeight;

                // 🔥 ここが最重要（1回だけ確保）
                if (_buffer == null || _buffer.Length != size)
                {
                    _buffer = new byte[size];
                }

                bmp.CopyPixels(_buffer, stride, 0);

                _ffmpeg.StandardInput.BaseStream.Write(_buffer, 0, size);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("AddFrame error: " + ex.Message);
            }
        }

        public async Task StopAsync()
        {
            if (_isStopping) return;
            _isStopping = true;

            try
            {
                if (_ffmpeg == null) return;

                await _ffmpeg.StandardInput.BaseStream.FlushAsync();
                _ffmpeg.StandardInput.Close();

                await Task.Run(() => _ffmpeg.WaitForExit());
            }
            catch { }

            try { _ffmpeg?.Dispose(); } catch { }

            _ffmpeg = null;
        }

        public void WriteRaw(byte[] buffer)
        {
            if (_isStopping || _ffmpeg == null || _ffmpeg.HasExited)
                return;

            try
            {
                _ffmpeg.StandardInput.BaseStream.Write(buffer, 0, buffer.Length);
            }
            catch { }
        }
    }
}