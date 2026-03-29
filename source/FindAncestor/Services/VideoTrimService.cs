using System;
using System.Diagnostics;
using System.Threading.Tasks;
using FindAncestor.ErrorDialog;

namespace FindAncestor.Services
{
    public class VideoTrimService
    {
        private readonly string _ffmpegPath = @"C:\Tools\ffmpeg\bin\ffmpeg.exe";

        public async Task TrimAsync(string input, string output, TimeSpan start, TimeSpan end)
        {
            try
            {
                if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(output))
                {
                    ErrorDialogService.Show("トリミング: パス不正");
                    return;
                }

                if (end <= start)
                {
                    ErrorDialogService.Show("トリミング: 終了時間が開始時間以下");
                    return;
                }

                string args =
                    $"-y -i \"{input}\" " +
                    $"-ss {start} -to {end} " +
                    "-c copy " +
                    $"\"{output}\"";

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = _ffmpegPath,
                        Arguments = args,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardError = true
                    }
                };

                process.Start();

                string error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    ErrorDialogService.Show("トリミング失敗\n" + error);
                }
            }
            catch (Exception ex)
            {
                ErrorDialogService.Show(ex);
            }
        }
    }
}