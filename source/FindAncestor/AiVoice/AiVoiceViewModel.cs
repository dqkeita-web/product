// AiVoice/ViewModels/AiVoiceViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FindAncestor.AiVoice.Services;
using FindAncestor.AiVoice.Interfaces;
using System;
using System.Windows;
using Microsoft.Win32;

namespace FindAncestor.AiVoice.ViewModels
{
    public partial class AiVoiceViewModel : ObservableObject
    {
        private readonly AiVoiceController _aiVoice = new();
        private readonly AiVoicePipeline _pipeline; [ObservableProperty] private bool _isRecording; [ObservableProperty] private string _resultText = "未実行"; public AiVoiceViewModel() { IAudioRecorder recorder = new BasicAudioRecorder(); IAudioFeatureExtractor extractor = new MfccFeatureExtractor(); IAiVoiceModel model = new SimpleAiVoiceModel(); _pipeline = new AiVoicePipeline(recorder, extractor, model); }

        [RelayCommand]
        private async Task ToggleRecording()
        {
            try
            {
                if (IsRecording)
                {
                    var result = _pipeline.StopAndPredict();
                    ResultText = result;
                    IsRecording = false;
                }
                else
                {
                    try
                    {
                        _pipeline.StartRecording();
                        ResultText = "録音中...";
                        IsRecording = true;
                    }
                    catch
                    {
                        // 🔥 録音デバイスなし → フォールバック
                        var res = MessageBox.Show(
                            "録音デバイスが見つかりません。\n\n設定 → サウンド → 入力 でマイクを確認してください。\n\n代わりに動画ファイルから音声抽出しますか？",
                            "マイク未検出",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning);

                        if (res != MessageBoxResult.Yes) return;

                        var dialog = new OpenFileDialog
                        {
                            Filter = "MP4 Files|*.mp4"
                        };

                        if (dialog.ShowDialog() != true) return;

                        ResultText = "AI音声生成中...";

                        // 🔥 あなたの既存パイプライン呼び出し
                        await _aiVoice.RunAsync(dialog.FileName);

                        ResultText = "AI音声モデル生成 完了";
                    }
                }
            }
            catch (Exception ex)
            {
                FindAncestor.ErrorDialog.ErrorDialogService.Show(ex);
                IsRecording = false;
            }
        }
    }
}