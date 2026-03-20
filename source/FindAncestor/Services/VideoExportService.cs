using System.Diagnostics;
using System.IO;

namespace FindAncestor.Services
{
    public partial class VideoExportService
    {
        private Process? _ffmpeg;
        private Stream? _inputStream;

        public void Start(int width, int height, int fps, string outputPath)
        {
            string args =
                $"-y -f rawvideo -pix_fmt bgra -s {width}x{height} -r {fps} -i - " +
                "-c:v libx264 -preset medium -crf 23 -pix_fmt yuv420p " +
                "-movflags +faststart " +
                $"\"{outputPath}\"";

            _ffmpeg = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = args,
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            _ffmpeg.Start();
            _inputStream = _ffmpeg.StandardInput.BaseStream;
        }

        public void WriteFrame(byte[] buffer)
        {
            _inputStream?.Write(buffer, 0, buffer.Length);
        }

        public void Stop()
        {
            _inputStream?.Flush();
            _inputStream?.Close();
            _ffmpeg?.WaitForExit();

            _inputStream = null;
            _ffmpeg?.Dispose();
            _ffmpeg = null;
        }
    }
}