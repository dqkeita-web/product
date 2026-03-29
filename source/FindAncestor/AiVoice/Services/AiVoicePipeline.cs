// AiVoice/Services/AiVoicePipeline.cs
using FindAncestor.AiVoice.Interfaces;
namespace FindAncestor.AiVoice.Services
{
    public class AiVoicePipeline { private readonly IAudioRecorder _recorder; private readonly IAudioFeatureExtractor _extractor; private readonly IAiVoiceModel _model; public AiVoicePipeline(IAudioRecorder recorder, IAudioFeatureExtractor extractor, IAiVoiceModel model) { _recorder = recorder; _extractor = extractor; _model = model; } public void StartRecording() { _recorder.StartRecording(); } public string StopAndPredict() { var wav = _recorder.StopRecording(); if (wav == null || wav.Length == 0) return "[録音データなし]"; var features = _extractor.ExtractFeatures(wav); return _model.Predict(features); } }
}