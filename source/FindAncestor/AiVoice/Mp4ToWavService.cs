// Voice/AiVoice/Mp4ToWavService.cs
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using FindAncestor.ErrorDialog;
namespace FindAncestor.AiVoice { public class Mp4ToWavService { private readonly string _ffmpegPath = @"C:\Tools\ffmpeg\bin\ffmpeg.exe"; public async Task ConvertAsync(string inputMp4, string outputWav) { try { if (!File.Exists(inputMp4)) { ErrorDialogHelper.Show("MP4が存在しません"); return; } Directory.CreateDirectory(Path.GetDirectoryName(outputWav)!); var args = $"-y -i \"{inputMp4}\" -vn -acodec pcm_s16le -ar 44100 -ac 1 \"{outputWav}\""; var process = new Process { StartInfo = new ProcessStartInfo { FileName = _ffmpegPath, Arguments = args, UseShellExecute = false, RedirectStandardError = true, CreateNoWindow = true } }; process.Start(); await Task.Run(() => process.WaitForExit()); } catch (Exception ex) { ErrorDialogService.Show(ex); } } } }