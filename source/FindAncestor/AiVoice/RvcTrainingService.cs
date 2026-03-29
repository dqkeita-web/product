using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using FindAncestor.ErrorDialog;

namespace FindAncestor.AiVoice
{
    public class RvcTrainingService
    {
        // 🔥 必ず自分の環境に合わせる
        private readonly string _pythonExe = @"C:\rvc\venv\Scripts\python.exe";
        private readonly string _trainScript = @"C:\rvc\train.py";

        public async Task TrainAsync(string datasetDir, string outputDir)
        {
            try
            {
                // =========================
                // 前提チェック
                // =========================
                if (!File.Exists(_pythonExe))
                {
                    ErrorDialogHelper.Show("Pythonが見つかりません:\n" + _pythonExe);
                    return;
                }

                if (!File.Exists(_trainScript))
                {
                    ErrorDialogHelper.Show("train.pyが見つかりません:\n" + _trainScript);
                    return;
                }

                if (!Directory.Exists(datasetDir))
                {
                    ErrorDialogHelper.Show("学習データが存在しません:\n" + datasetDir);
                    return;
                }

                var wavs = Directory.GetFiles(datasetDir, "*.wav", SearchOption.AllDirectories);
                if (wavs.Length == 0)
                {
                    ErrorDialogHelper.Show("学習用wavが0件です");
                    return;
                }

                Directory.CreateDirectory(outputDir);

                // =========================
                // コマンド生成
                // =========================
                var args =
                    $"\"{_trainScript}\" " +
                    $"--dataset \"{datasetDir}\" " +
                    $"--output \"{outputDir}\" " +
                    $"--gpu 0";

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = _pythonExe,
                        Arguments = args,
                        WorkingDirectory = Path.GetDirectoryName(_trainScript)!,
                        UseShellExecute = false,
                        RedirectStandardError = true,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };

                // =========================
                // ログ（全部出す）
                // =========================
                process.OutputDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        Debug.WriteLine("RVC OUT: " + e.Data);
                };

                process.ErrorDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        Debug.WriteLine("RVC ERR: " + e.Data);
                };

                // =========================
                // 実行
                // =========================
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                await Task.Run(() => process.WaitForExit());

                // =========================
                // 終了チェック
                // =========================
                if (process.ExitCode != 0)
                {
                    ErrorDialogHelper.Show("RVC学習失敗 ExitCode=" + process.ExitCode);
                    return;
                }

                // =========================
                // 出力チェック（超重要）
                // =========================
                var pth = Directory.GetFiles(outputDir, "*.pth", SearchOption.AllDirectories);
                var index = Directory.GetFiles(outputDir, "*.index", SearchOption.AllDirectories);

                if (pth.Length == 0 || index.Length == 0)
                {
                    ErrorDialogHelper.Show("モデル生成失敗（pth/indexなし）");
                    return;
                }

                // =========================
                // 成功
                // =========================
                ErrorDialogHelper.Show("AI音声モデル生成 完了");
            }
            catch (Exception ex)
            {
                ErrorDialogService.Show(ex);
            }
        }
    }
}