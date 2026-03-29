// AiVoice/Interfaces/IAudioRecorder.cs
using System;
namespace FindAncestor.AiVoice.Interfaces { public interface IAudioRecorder : IDisposable { void StartRecording(); byte[] StopRecording(); bool IsRecording { get; } } }