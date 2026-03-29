// Voice/AiVoice/AudioSplitService.cs
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using FindAncestor.ErrorDialog;
namespace FindAncestor.AiVoice { public class AudioSplitService { private readonly string _ffmpegPath = @"C:\Tools\ffmpeg\bin\ffmpeg.exe"; public async Task SplitAsync(string input, string output, int sec = 10) { try { Directory.CreateDirectory(output); var pattern = Path.Combine(output, "seg_%03d.wav"); var args = $"-y -i \"{input}\" -f segment -segment_time {sec} -c copy \"{pattern}\""; var p = new Process { StartInfo = new ProcessStartInfo { FileName = _ffmpegPath, Arguments = args, UseShellExecute = false, RedirectStandardError = true, CreateNoWindow = true } }; p.Start(); await Task.Run(() => p.WaitForExit()); } catch (Exception ex) { ErrorDialogService.Show(ex); } } } }