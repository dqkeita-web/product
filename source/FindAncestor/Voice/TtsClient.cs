using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FindAncestor.ErrorDialog;

namespace FindAncestor.Voice
{
    public class TtsClient
    {
        private readonly HttpClient _http;
        private readonly string _baseUrl;
        private readonly string _voicevoxExePath;

        public TtsClient(string baseUrl, string voicevoxExePath = "")
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _http = new HttpClient();
            _voicevoxExePath = voicevoxExePath;
        }

        // =========================
        // ★ VOICEVOX接続保証
        // =========================
        private async Task EnsureServerAsync(CancellationToken ct)
        {
            if (await IsServerAlive(ct)) return;

            // 起動試行
            TryStartVoiceVox();

            // 最大5秒待機
            var timeout = DateTime.UtcNow.AddSeconds(5);

            while (DateTime.UtcNow < timeout)
            {
                if (await IsServerAlive(ct)) return;
                await Task.Delay(300, ct);
            }

            throw new Exception(
                "VOICEVOXに接続できません。\n" +
                "・VOICEVOXが起動していない\n" +
                "・ポートが違う（50021）\n" +
                $"URL: {_baseUrl}"
            );
        }

        private async Task<bool> IsServerAlive(CancellationToken ct)
        {
            try
            {
                using var res = await _http.GetAsync($"{_baseUrl}/version", ct);
                return res.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private void TryStartVoiceVox()
        {
            if (string.IsNullOrWhiteSpace(_voicevoxExePath)) return;

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = _voicevoxExePath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                ErrorDialogService.Show("VOICEVOX起動失敗:\n" + ex);
            }
        }

        // =========================
        // ★ 音声合成
        // =========================
        public async Task<byte[]> SynthesizeAsync(string text, VoiceParameter param, CancellationToken ct = default)
        {
            try
            {
                await EnsureServerAsync(ct);

                // =========================
                // audio_query
                // =========================
                var queryUrl = $"{_baseUrl}/audio_query?text={Uri.EscapeDataString(text)}&speaker={param.SpeakerId}";
                using var queryResponse = await _http.PostAsync(queryUrl, null, ct);
                queryResponse.EnsureSuccessStatusCode();

                var queryJson = await queryResponse.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(queryJson);
                var root = doc.RootElement.Clone();

                // =========================
                // パラメータ適用
                // =========================
                using var ms = new System.IO.MemoryStream();
                using (var writer = new Utf8JsonWriter(ms))
                {
                    writer.WriteStartObject();

                    foreach (var prop in root.EnumerateObject())
                    {
                        if (prop.NameEquals("speedScale"))
                            writer.WriteNumber("speedScale", param.Speed);
                        else if (prop.NameEquals("pitchScale"))
                            writer.WriteNumber("pitchScale", param.Pitch);
                        else if (prop.NameEquals("intonationScale"))
                            writer.WriteNumber("intonationScale", 1.13);
                        else if (prop.NameEquals("volumeScale"))
                            writer.WriteNumber("volumeScale", param.Volume);
                        else
                            prop.WriteTo(writer);
                    }

                    writer.WriteEndObject();
                }

                var modifiedJson = Encoding.UTF8.GetString(ms.ToArray());

                // =========================
                // synthesis
                // =========================
                var synthUrl = $"{_baseUrl}/synthesis?speaker={param.SpeakerId}";
                using var content = new StringContent(modifiedJson, Encoding.UTF8, "application/json");
                using var synthResponse = await _http.PostAsync(synthUrl, content, ct);
                synthResponse.EnsureSuccessStatusCode();

                return await synthResponse.Content.ReadAsByteArrayAsync(ct);
            }
            catch (Exception ex)
            {
                ErrorDialogService.Show("TTS生成失敗:\n" + ex);
                throw;
            }
        }
    }
}