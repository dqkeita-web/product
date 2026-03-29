using FindAncestor.ErrorDialog;
using FindAncestor.Voice;
using FindAncestor.Voice.OCR;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using System.Windows;

namespace FindAncestor
{
    public partial class App : Application
    {
        public static IServiceProvider Services { get; private set; } = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                var services = new ServiceCollection();

                // =========================
                // OCR
                // =========================
                services.AddSingleton<IOcrService, OcrService>();
                services.AddSingleton<OcrCacheService>();
                services.AddSingleton<OcrPipelineService>();

                // =========================
                // Voice Core
                // =========================
                services.AddSingleton<VoicePlaybackService>();

                services.AddSingleton<TtsClient>(_ =>
                    new TtsClient("http://localhost:50021"));

                services.AddSingleton<PythonEmotionClient>(_ =>
                    new PythonEmotionClient(
                        httpEndpoint: null,
                        pythonExe: "python",
                        scriptPath: "emotion.py"));

                services.AddSingleton<CharacterVoiceService>(_ =>
                {
                    var config = VoiceConfigLoader.Load("voice_config.json");
                    return new CharacterVoiceService(config);
                });

                // 🔥 ここが原因だった
                services.AddSingleton<VoicePipelineService>();

                Services = services.BuildServiceProvider();
            }
            catch (Exception ex)
            {
                ErrorDialogService.Show(ex);
                Shutdown();
                return;
            }

            DispatcherUnhandledException += (s, ex) =>
            {
                ErrorDialogService.Show(ex.Exception);
                ex.Handled = true;
            };

            AppDomain.CurrentDomain.UnhandledException += (s, ex) =>
            {
                if (ex.ExceptionObject is Exception e2)
                    ErrorDialogService.Show(e2);
            };
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            try
            {
                foreach (var p in Process.GetProcessesByName("ffmpeg"))
                {
                    p.Kill();
                }
            }
            catch { }
        }
    }
}