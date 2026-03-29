// Voice/AiVoice/AiVoiceModel.cs
using System;
using System.Diagnostics;
using System.IO;

namespace FindAncestor.AiVoice
{ 
    public class AiVoiceModel
    { 
        public string InputMp4Path { get; set; } = string.Empty;
        public string WorkingDirectory { get; set; } = string.Empty;
    public string WavPath => System.IO.Path.Combine(WorkingDirectory, "audio.wav");
    public string CleanPath => System.IO.Path.Combine(WorkingDirectory, "clean.wav");
    public string DatasetPath => System.IO.Path.Combine(WorkingDirectory, "dataset"); 
    public string ModelOutputPath => System.IO.Path.Combine(WorkingDirectory, "model");
   
    }
}