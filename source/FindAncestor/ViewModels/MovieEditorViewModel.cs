using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FindAncestor.Enum;
using FindAncestor.Models;
using FindAncestor.Services;
using FindAncestor.Views;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace FindAncestor.ViewModels;

public partial class MovieEditorViewModel : ObservableObject
{
    private ScrollingPreviewViewModel? _scrollViewModel;

    // =========================
    // 基本設定
    // =========================

    [ObservableProperty] private DisplaySize? _selectedDisplaySize;
    [ObservableProperty] private bool _isPresetMode;
    [ObservableProperty] private AspectRatioItem _selectedAspectRatio = null!;
    [ObservableProperty] private double _imageWidth = 900;
    [ObservableProperty] private double _scrollSpeed = 3;

    public ObservableCollection<DisplaySize> DisplaySizes { get; } =
    [
        new DisplaySize { Name = "HD", Width = 1280 },
        new DisplaySize { Name = "FullHD", Width = 1920, Height = 1080 },
        new DisplaySize { Name = "2K", Width = 2560, Height = 1440 },
        new DisplaySize { Name = "4K", Width = 3840, Height = 2160 }
    ];

    public ObservableCollection<AspectRatioItem> AspectRatios { get; } =
    [
        new AspectRatioItem("16:9", 16.0/9.0),
        new AspectRatioItem("4:3", 4.0/3.0),
        new AspectRatioItem("1:1", 1.0),
        new AspectRatioItem("3:2", 3.0/2.0)
    ];

    public MovieEditorViewModel()
    {
        if (AspectRatios.Count > 0)
            SelectedAspectRatio = AspectRatios[0];
    }

    // =========================
    // UI制御
    // =========================

    [ObservableProperty] private double _uiOpacity = 1;
    [ObservableProperty] private double _settingPanelX = 0;
    [ObservableProperty] private bool _isPlaying;

    [RelayCommand]
    private void ToggleSettingPanel()
    {
        SettingPanelX = SettingPanelX == 0 ? -280 : 0;
    }

    [RelayCommand]
    private static void Close()
    {
        Application.Current.Shutdown();
    }

    // =========================
    // 画面サイズ
    // =========================

    [RelayCommand]
    private void SelectDisplaySize(DisplaySize size)
    {
        if (SelectedDisplaySize == size)
        {
            SelectedDisplaySize = null;
            IsPresetMode = false;
            return;
        }

        SelectedDisplaySize = size;
        IsPresetMode = true;
        ImageWidth = size.Width;
    }

    partial void OnImageWidthChanged(double value)
    {
        UpdatePreviewSize(value, SelectedAspectRatio.Value);
    }

    private void UpdatePreviewSize(double width, double aspect)
    {
        _scrollViewModel?.UpdateSize(width, aspect);
        EmbeddedPreviewVM?.UpdateSize(width, aspect);
    }

    private void UpdateScrollSpeedAll(double speed)
    {
        _scrollViewModel?.UpdateScrollSpeed(speed);
        EmbeddedPreviewVM?.UpdateScrollSpeed(speed);
    }

    partial void OnScrollSpeedChanged(double value)
    {
        UpdateScrollSpeedAll(value);
    }

    partial void OnSelectedAspectRatioChanged(AspectRatioItem value)
    {
        UpdatePreviewSize(ImageWidth, value.Value);
    }

    // =========================
    // 音声
    // =========================

    [ObservableProperty] private ObservableCollection<string> _audioFiles = [];
    [ObservableProperty] private int _currentAudioIndex;
    [ObservableProperty] private bool _isLoopEnabled = true;
    [ObservableProperty] private double _fadeDuration = 1.5;

    [RelayCommand]
    private void SelectAudio()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Audio Files|*.mp3;*.wav",
            Multiselect = true
        };

        if (dialog.ShowDialog() == true)
        {
            AudioFiles.Clear();
            foreach (var file in dialog.FileNames)
                AudioFiles.Add(file);

            CurrentAudioIndex = 0;
        }
    }

    private void PlayAudio()
    {
        if (_scrollViewModel == null || AudioFiles.Count == 0) return;

        _scrollViewModel.StartAudio(
            AudioFiles[CurrentAudioIndex],
            IsLoopEnabled,
            FadeDuration);
    }

    private void StopAudio()
    {
        _scrollViewModel?.StopAudio(FadeDuration);
    }

    [RelayCommand]
    private void TogglePlay()
    {
        if (IsPlaying)
        {
            StopAudio();
            IsPlaying = false;
        }
        else
        {
            PlayAudio();
            IsPlaying = true;
        }
    }

    [RelayCommand]
    private void NextAudio()
    {
        if (AudioFiles.Count == 0) return;

        CurrentAudioIndex = (CurrentAudioIndex + 1) % AudioFiles.Count;
        PlayAudio();
    }

    [RelayCommand]
    private void PrevAudio()
    {
        if (AudioFiles.Count == 0) return;

        CurrentAudioIndex--;
        if (CurrentAudioIndex < 0)
            CurrentAudioIndex = AudioFiles.Count - 1;

        PlayAudio();
    }

    // =========================
    // プレビュー生成
    // =========================

    private void CreatePreview(out double width, out double height)
    {
        if (IsPresetMode && SelectedDisplaySize != null)
        {
            width = SelectedDisplaySize.Width;
            height = SelectedDisplaySize.Height;
        }
        else
        {
            width = ImageWidth;
            height = ImageWidth / SelectedAspectRatio.Value;
        }
    }

    // =========================
    // Window再生
    // =========================

    [RelayCommand]
    private void OpenScroll1Row()
    {
        CreatePreview(out var width, out var height);

        _scrollViewModel = new ScrollingPreviewViewModel(
            height,
            SelectedAspectRatio.Value,
            ScrollSpeed);

        var window = new ShowMovie
        {
            Width = width,
            Height = height,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            DataContext = _scrollViewModel
        };

        window.Show();
    }

    // =========================
    // 埋め込み再生
    // =========================

    [ObservableProperty]
    private bool _isEmbeddedMode;

    [ObservableProperty]
    private ScrollingPreviewViewModel? _embeddedPreviewVM;

    [RelayCommand]
    private void OpenScrollEmbedded()
    {
        CreatePreview(out var width, out var height);

        EmbeddedPreviewVM = new ScrollingPreviewViewModel(
            height,
            SelectedAspectRatio.Value,
            ScrollSpeed);

        IsEmbeddedMode = true;
    }

    [RelayCommand]
    private void CloseEmbedded()
    {
        IsEmbeddedMode = false;
        EmbeddedPreviewVM = null;
    }
}