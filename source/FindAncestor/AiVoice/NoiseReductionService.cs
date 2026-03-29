using FindAncestor.ErrorDialog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace FindAncestor.AiVoice
{
    public class NoiseReductionService
    {
        private readonly string _ffmpegPath = @"C:\Tools\ffmpeg\bin\ffmpeg.exe";

        public async Task CleanAsync(string inputWav, string outputWav)
        {
            try
            {
                if (!File.Exists(inputWav))
                {
                    ErrorDialogService.Show("入力音声が存在しません");
                    return;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(outputWav)!);

                var args =
                    $"-y -i \"{inputWav}\" " +
                    "-af \"afftdn,silenceremove=1:0:-50dB,loudnorm\" " +
                    $"\"{outputWav}\"";

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = _ffmpegPath,
                        Arguments = args,
                        UseShellExecute = false,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.ErrorDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data) &&
                        (e.Data.Contains("Error") || e.Data.Contains("failed")))
                    {
                        ErrorDialogService.Show("ノイズ除去エラー\n" + e.Data);
                    }
                };

                process.Start();
                process.BeginErrorReadLine();

                await Task.Run(() => process.WaitForExit());

                if (!File.Exists(outputWav))
                {
                    ErrorDialogService.Show("クリーン音声生成失敗");
                }
            }
            catch (Exception ex)
            {
                ErrorDialogService.Show(ex);
            }
        }
    }
}
