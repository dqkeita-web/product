using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FindAncestor.Enum;
using FindAncestor.Models;
using FindAncestor.Services;
using FindAncestor.Views;
using Microsoft.Win32;

namespace FindAncestor.ViewModels
{
    public partial class MovieEditorViewModel : ObservableObject
    {
        private readonly ImageStorageService _imageService = new();
        private readonly VideoExportService _videoService = new();

        private ScrollingPreviewViewModel? _scrollViewModel;

        [ObservableProperty] private DisplaySize? _selectedDisplaySize;
        [ObservableProperty] private bool _isPresetMode = false;
        [ObservableProperty] private ImageFolderType _selectedImageFolder = ImageFolderType.A;
        [ObservableProperty] private ImageSaveFormat _selectedImageSaveFormat = ImageSaveFormat.Png;

        [ObservableProperty] private ObservableCollection<string> audioFiles = new();
        [ObservableProperty] private int currentAudioIndex = 0;
        [ObservableProperty] private string currentAudioFileName = "";
        [ObservableProperty] private bool isLoopEnabled = true;
        [ObservableProperty] private double fadeDuration = 1.5;
        [ObservableProperty] private int _audioVolume = 70;

        [ObservableProperty] private ImageExportFormat _selectedFormat = ImageExportFormat.Png;
        [ObservableProperty] private double _scrollSpeed = 3;
        [ObservableProperty] private AspectRatioItem _selectedAspectRatio = null!;
        [ObservableProperty] private double _imageWidth = 900;
        [ObservableProperty] private int _exportDurationSeconds = 10;

        public ObservableCollection<ImageFolderType> ImageFolders { get; } = new()
        {
            ImageFolderType.A, ImageFolderType.B, ImageFolderType.C, ImageFolderType.D
        };

        public ObservableCollection<ImageSaveFormat> ImageSaveFormats { get; } = new()
        {
            ImageSaveFormat.Png, ImageSaveFormat.Jpeg
        };

        public ObservableCollection<DisplaySize> DisplaySizes { get; } = new()
        {
            new DisplaySize { Name = "HD", Width = 1280 },
            new DisplaySize { Name = "FullHD", Width = 1920, Height = 1080 },
            new DisplaySize { Name = "2K", Width = 2560, Height = 1440 },
            new DisplaySize { Name = "4K", Width = 3840, Height = 2160 }
        };

        public ObservableCollection<AspectRatioItem> AspectRatios { get; } = new()
        {
            new AspectRatioItem("16:9", 16.0/9.0),
            new AspectRatioItem("4:3", 4.0/3.0),
            new AspectRatioItem("1:1", 1.0),
            new AspectRatioItem("3:2", 3.0/2.0)
        };

        public MovieEditorViewModel()
        {
            if (AspectRatios.Count > 0)
                SelectedAspectRatio = AspectRatios[0];
        }

        // =========================
        // UI連動
        // =========================

        partial void OnSelectedDisplaySizeChanged(DisplaySize? value)
        {
            if (value == null) return;

            IsPresetMode = true;
            ImageWidth = value.Width;
        }

        partial void OnImageWidthChanged(double value)
        {
            if (IsPresetMode)
            {
                IsPresetMode = false;
                SelectedDisplaySize = null;
            }

            _scrollViewModel?.UpdateSize(value, SelectedAspectRatio.Value);
        }

        partial void OnScrollSpeedChanged(double value)
        {
            _scrollViewModel?.UpdateScrollSpeed(value);
        }

        partial void OnSelectedAspectRatioChanged(AspectRatioItem value)
        {
            _scrollViewModel?.UpdateSize(ImageWidth, value.Value);
        }

        // =========================
        // コマンド（全部復元）
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

        [RelayCommand]
        private void AddImageToFolder()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Image Files|*.png;*.jpg;*.jpeg",
                Multiselect = true
            };

            if (dialog.ShowDialog() != true) return;

            _imageService.SaveImages(dialog.FileNames, SelectedImageFolder, SelectedImageSaveFormat);

            MessageBox.Show("画像追加完了");
        }

        [RelayCommand]
        private void DeleteImagesInFolder()
        {
            _imageService.DeleteImages(SelectedImageFolder);
        }

        [RelayCommand]
        private void SelectAudio()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Audio Files|*.mp3;*.wav;*.m4a;*.wma",
                Multiselect = true
            };

            if (dialog.ShowDialog() == true)
            {
                AudioFiles.Clear();

                foreach (var file in dialog.FileNames)
                    AudioFiles.Add(file);

                if (AudioFiles.Count > 0)
                {
                    CurrentAudioIndex = 0;
                    CurrentAudioFileName = Path.GetFileName(AudioFiles[0]);
                }
            }
        }

        [RelayCommand]
        private void PlayAudio()
        {
            if (_scrollViewModel == null || AudioFiles.Count == 0) return;

            _scrollViewModel.StartAudio(
                AudioFiles[CurrentAudioIndex],
                IsLoopEnabled,
                FadeDuration);
        }

        [RelayCommand]
        private void StopAudio()
        {
            _scrollViewModel?.StopAudio(FadeDuration);
        }

        [RelayCommand]
        private void NextAudio()
        {
            if (AudioFiles.Count == 0) return;

            CurrentAudioIndex = (CurrentAudioIndex + 1) % AudioFiles.Count;
            CurrentAudioFileName = Path.GetFileName(AudioFiles[CurrentAudioIndex]);

            PlayAudio();
        }

        [RelayCommand]
        private void PrevAudio()
        {
            if (AudioFiles.Count == 0) return;

            CurrentAudioIndex--;
            if (CurrentAudioIndex < 0)
                CurrentAudioIndex = AudioFiles.Count - 1;

            CurrentAudioFileName = Path.GetFileName(AudioFiles[CurrentAudioIndex]);

            PlayAudio();
        }

        [RelayCommand]
        private void OpenScroll1Row()
        {
            double width;
            double height;

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

            var window = new ShowMovie
            {
                Width = width,
                Height = height,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            _scrollViewModel = new ScrollingPreviewViewModel(
                height,
                SelectedAspectRatio.Value,
                ScrollSpeed);

            window.DataContext = _scrollViewModel;
            window.Show();
        }

        [RelayCommand]
        private async void ExportPng()
        {
            SelectedFormat = ImageExportFormat.Png;
            await Export();
        }

        [RelayCommand]
        private async void ExportJpeg()
        {
            SelectedFormat = ImageExportFormat.Jpeg;
            await Export();
        }

        private async Task Export()
        {
            if (_scrollViewModel == null) return;

            await _videoService.ExportAsync(
                _scrollViewModel,
                SelectedFormat,
                ExportDurationSeconds,
                SelectedDisplaySize,
                IsPresetMode,
                ImageWidth,
                SelectedAspectRatio,
                AudioFiles,
                CurrentAudioIndex);
        }
    }
}