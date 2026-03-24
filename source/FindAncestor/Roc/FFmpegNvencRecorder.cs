using System.Diagnostics;
using FindAncestor.ErrorDialog;

namespace FindAncestor.Roc
{
    public class FFmpegNvencRecorder
    {
        private Process? _ffmpeg;
        private volatile bool _isStopping;

        private readonly string ffmpegPath = @"C:\Tools\ffmpeg\bin\ffmpeg.exe";

        public void Start(string outputPath, int width, int height, int fps, string? audioPath)
        {
            try
            {
                _isStopping = false;

                string args =
                    $"-y -f rawvideo -pix_fmt bgra -s {width}x{height} -r {fps} -i - " +
                    "-c:v libx264 -preset veryfast -crf 23 " +
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

                if (!_ffmpeg.Start())
                {
                    ErrorDialogHelper.Show("❌ FFmpeg起動失敗");
                    return;
                }

                _ffmpeg.ErrorDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        Debug.WriteLine("FFmpeg: " + e.Data);

                        if (e.Data.Contains("Error") || e.Data.Contains("failed"))
                        {
                            ErrorDialogHelper.Show("❌ FFmpegエラー\n" + e.Data);
                        }
                    }
                };

                _ffmpeg.BeginErrorReadLine();
            }
            catch (Exception ex)
            {
                ErrorDialogHelper.Show("❌ FFmpeg.Start例外\n" + ex);
            }
        }

        public void WriteRaw(byte[] buffer)
        {
            try
            {
                if (_isStopping || _ffmpeg == null || _ffmpeg.HasExited)
                    return;

                _ffmpeg.StandardInput.BaseStream.Write(buffer, 0, buffer.Length);
            }
            catch (Exception ex)
            {
                ErrorDialogHelper.Show("❌ WriteRaw例外\n" + ex.Message);
            }
        }

        public async Task StopAsync()
        {
            try
            {
                if (_isStopping)
                    return;

                _isStopping = true;

                if (_ffmpeg == null)
                    return;

                await _ffmpeg.StandardInput.BaseStream.FlushAsync();
                _ffmpeg.StandardInput.Close();

                await Task.Run(() => _ffmpeg.WaitForExit());

                _ffmpeg.Dispose();
                _ffmpeg = null;
            }
            catch (Exception ex)
            {
                ErrorDialogHelper.Show("❌ FFmpeg.Stop例外\n" + ex);
            }
        }
    }
}