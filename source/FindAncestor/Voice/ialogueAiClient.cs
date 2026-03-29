using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FindAncestor.Voice
{
    public class DialogueAiClient
    {
        public async Task<DialogueResult> GenerateAsync(string text, CancellationToken ct)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = $"voice_dialogue.py \"{text}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var p = Process.Start(psi);
            string json = await p.StandardOutput.ReadToEndAsync();

            return JsonSerializer.Deserialize<DialogueResult>(json)
                   ?? new DialogueResult { Text = text };
        }
    }

    public class DialogueResult
    {
        public string Text { get; set; } = "";
        public string Style { get; set; } = "normal";
        public double Tempo { get; set; } = 1.0;
    }
}