using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FindAncestor.Voice
{
    public class PythonEmotionClient
    {
        private readonly HttpClient _http;
        private readonly string? _endpoint;
        private readonly string? _pythonExe;
        private readonly string? _scriptPath;
        private readonly int _timeoutMs;

        public PythonEmotionClient(
            string? httpEndpoint = null,
            string? pythonExe = null,
            string? scriptPath = null,
            int timeoutMs = 10000)
        {
            _endpoint = httpEndpoint;
            _pythonExe = pythonExe;
            _scriptPath = scriptPath;
            _timeoutMs = timeoutMs;

            _http = new HttpClient
            {
                Timeout = TimeSpan.FromMilliseconds(timeoutMs)
            };
        }

        // =========================
        // Public API
        // =========================
        public async Task<EmotionAnalysisResult> AnalyzeAsync(string text, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(text))
                return CreateFallback(text);

            // 1. HTTP優先
            if (!string.IsNullOrWhiteSpace(_endpoint))
            {
                try
                {
                    return await AnalyzeHttpAsync(text, ct);
                }
                catch
                {
                    // フォールバックへ
                }
            }

            // 2. ローカルPython
            if (!string.IsNullOrWhiteSpace(_pythonExe) && !string.IsNullOrWhiteSpace(_scriptPath))
            {
                try
                {
                    return await AnalyzeProcessAsync(text, ct);
                }
                catch
                {
                    // フォールバックへ
                }
            }

            // 3. フォールバック
            return CreateFallback(text);
        }

        // =========================
        // HTTP実行
        // =========================
        private async Task<EmotionAnalysisResult> AnalyzeHttpAsync(string text, CancellationToken ct)
        {
            var payload = new
            {
                text = text
            };

            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var res = await _http.PostAsync(_endpoint!, content, ct);
            res.EnsureSuccessStatusCode();

            var resJson = await res.Content.ReadAsStringAsync(ct);

            var result = JsonSerializer.Deserialize<EmotionAnalysisResult>(resJson);

            if (result == null)
                throw new Exception("HTTPレスポンスが不正");

            Normalize(result, text);

            return result;
        }

        // =========================
        // プロセス実行
        // =========================
        private async Task<EmotionAnalysisResult> AnalyzeProcessAsync(string text, CancellationToken ct)
        {
            var psi = new ProcessStartInfo
            {
                FileName = _pythonExe!,
                Arguments = $"\"{_scriptPath}\"",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = psi };

            var stdout = new StringBuilder();
            var stderr = new StringBuilder();

            var tcs = new TaskCompletionSource<bool>();

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null) stdout.AppendLine(e.Data);
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null) stderr.AppendLine(e.Data);
            };

            if (!process.Start())
                throw new Exception("Python起動失敗");

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.StandardInput.WriteAsync(text);
            process.StandardInput.Close();

            using var timeoutCts = new CancellationTokenSource(_timeoutMs);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

            var waitTask = Task.Run(() =>
            {
                process.WaitForExit();
                tcs.TrySetResult(true);
            }, linkedCts.Token);

            var completed = await Task.WhenAny(tcs.Task, Task.Delay(_timeoutMs, linkedCts.Token));

            if (completed != tcs.Task)
            {
                try { process.Kill(); } catch { }
                throw new TimeoutException("Pythonタイムアウト");
            }

            if (process.ExitCode != 0)
            {
                throw new Exception($"Pythonエラー: {stderr}");
            }

            var json = stdout.ToString();

            var result = JsonSerializer.Deserialize<EmotionAnalysisResult>(json);

            if (result == null)
                throw new Exception("JSON解析失敗");

            Normalize(result, text);

            return result;
        }

        // =========================
        // 正規化
        // =========================
        private void Normalize(EmotionAnalysisResult result, string originalText)
        {
            if (result.Segments == null || result.Segments.Count == 0)
            {
                result.Segments = CreateFallback(originalText).Segments;
                return;
            }

            foreach (var seg in result.Segments)
            {
                if (string.IsNullOrWhiteSpace(seg.Text))
                    seg.Text = originalText;

                seg.Intensity = Clamp01(seg.Intensity);
                seg.Emphasis = Clamp01(seg.Emphasis);

                if (seg.Pause < 0)
                    seg.Pause = 0;

                if (string.IsNullOrWhiteSpace(seg.Emotion))
                    seg.Emotion = "neutral";
            }
        }

        // =========================
        // フォールバック
        // =========================
        private EmotionAnalysisResult CreateFallback(string text)
        {
            return new EmotionAnalysisResult
            {
                Segments =
                {
                    new EmotionAnalysisResult.SegmentEmotion
                    {
                        Text = text,
                        Emotion = "neutral",
                        Intensity = 0.5,
                        Emphasis = 0,
                        Pause = 0
                    }
                }
            };
        }

        // =========================
        // Utility
        // =========================
        private double Clamp01(double v)
        {
            if (v < 0) return 0;
            if (v > 1) return 1;
            return v;
        }
    }
}