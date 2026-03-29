// Voice/AiVoice/DemucsService.cs
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using FindAncestor.ErrorDialog;
namespace FindAncestor.AiVoice
{
    public class DemucsService
    {
        private readonly string _pythonExe = @"C:\rvc\venv\Scripts\python.exe";
        private readonly string _trainScript = @"C:\rvc\train.py";
        public async Task ExtractVocalsAsync(string inputWav, string outputDir) {
            try
            {
                Directory.CreateDirectory(outputDir);
                var args = $"-m demucs --two-stems=vocals \"{inputWav}\" -o \"{outputDir}\"";
                var p = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = _pythonExe, Arguments = args, UseShellExecute = false, RedirectStandardError = true,
                        CreateNoWindow = true

                    }
                };
                p.Start();
                await Task.Run(() => p.WaitForExit());

            }
            catch (Exception ex)
            {
                ErrorDialogService.Show(ex);

            }
        } 
        public string ResolveVocalPath(string outputDir, string inputWav) 
        { var name = Path.GetFileNameWithoutExtension(inputWav); var path = Path.Combine(outputDir, "htdemucs", name, "vocals.wav"); 
            return File.Exists(path) ? path : string.Empty; }
    }
}