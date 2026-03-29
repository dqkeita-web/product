// ファイル: VoicePipelineService.cs
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FindAncestor.Voice
{
    public class VoicePipelineService
    {
        private readonly TtsClient _tts;
        private readonly VoicePlaybackService _playback;
        private readonly PythonEmotionClient _emotion;
        private readonly VoiceVariationService _variation;
        private readonly TextValidationService _validator;

        public VoicePipelineService(
            TtsClient tts,
            VoicePlaybackService playback,
            PythonEmotionClient emotion,
            VoiceVariationService variation,
            TextValidationService validator)
        {
            _tts = tts;
            _playback = playback;
            _emotion = emotion;
            _variation = variation;
            _validator = validator;
        }

        public async Task SpeakLinesAsync(List<string> lines, CancellationToken ct = default)
        {
            foreach (var raw in lines)
            {
                var text = _validator.Validate(raw);
                if (string.IsNullOrWhiteSpace(text)) continue;

                var emo = await _emotion.AnalyzeAsync(text, ct);

                var param = new VoiceParameter
                {
                    SpeakerId = 1,
                    Pitch = 0.03f,   // ★若め
                    Speed = 0.92f,   // ★ゆっくり
                    Volume = 1.0f
                };

                param = _variation.Apply(
                    param,
                    emo.PrimaryEmotion,
                    emo.Score,
                    "cute"
                );

                var wav = await _tts.SynthesizeAsync(text, param, ct);

                await _playback.PlayAsync(wav, ct);

                // ★人間の「間」
                await Task.Delay(250, ct);
            }
        }
    }
}