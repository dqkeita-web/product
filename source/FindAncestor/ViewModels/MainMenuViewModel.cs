using CommunityToolkit.Mvvm.ComponentModel;

using CommunityToolkit.Mvvm.Input;
using FindAncestor.Enum;
using FindAncestor.Models;
using FindAncestor.Views;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using FindAncestor.Enum;



namespace FindAncestor.ViewModels
{
    public partial class MainMenuViewModel : ObservableObject

    {
        [ObservableProperty]
        private DisplaySize? _selectedDisplaySize;

        [ObservableProperty]
        private bool _isPresetMode = false; // ← プリセット使用中かどうか

        // 追加
        [ObservableProperty]
        private ImageFolderType _selectedImageFolder = ImageFolderType.A;

        [ObservableProperty]
        private ImageSaveFormat _selectedImageSaveFormat = ImageSaveFormat.Png;

        public ObservableCollection<ImageFolderType> ImageFolders { get; } =
            new ObservableCollection<ImageFolderType>
            {
                ImageFolderType.A,
                ImageFolderType.B,
                ImageFolderType.C,
                ImageFolderType.D
            };

        public ObservableCollection<ImageSaveFormat> ImageSaveFormats { get; } =
            new ObservableCollection<ImageSaveFormat>
            {
                ImageSaveFormat.Png,
                ImageSaveFormat.Jpeg
            };

        public ObservableCollection<DisplaySize> DisplaySizes { get; } =
            new ObservableCollection<DisplaySize>
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

        [ObservableProperty]
        private int _exportDurationSeconds = 10;  // デフォルト10秒

        [ObservableProperty]
        private ObservableCollection<string> audioFiles = new();

        [ObservableProperty]
        private int _audioVolume = 70;   // ← 0-100

        [ObservableProperty]
        private int currentAudioIndex = 0;

        [ObservableProperty]
        private string currentAudioFileName = "";

        [ObservableProperty]
        private bool isLoopEnabled = true;

        [ObservableProperty]
        private double fadeDuration = 1.5;

        [ObservableProperty]
        private string? _selectedAudioPath;

        [ObservableProperty]
        private ImageExportFormat _selectedFormat = ImageExportFormat.Png;

        [ObservableProperty]
        private double _scrollSpeed = 3; // 1～5

        [ObservableProperty]
        private AspectRatioItem _selectedAspectRatio = null!;

        [ObservableProperty]
        private double _imageWidth = 900;

        [ObservableProperty]
        private string _selectedAudioFileName = "";


        partial void OnSelectedDisplaySizeChanged(DisplaySize? value)
        {
            if (value == null) return;

            IsPresetMode = true;

            // プリセットの横幅をスライダーに反映
            ImageWidth = value.Width;
        }
        partial void OnAudioVolumeChanged(int value)
        {
            _scrollViewModel?.SetVolume(value / 100.0);
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

        // --- 横スクロール用 ---
        private DispatcherTimer _scrollTimer;
        private double _scrollPosition;
        private Scroll1RowViewModel? _scrollViewModel;
        public double ScrollPosition
        {
            get => _scrollPosition;
            set => SetProperty(ref _scrollPosition, value);
        }

        public MainMenuViewModel()
        {
            if (AspectRatios.Count > 0)
                SelectedAspectRatio = AspectRatios[0];
        }

        [RelayCommand]
        private void SelectAspectRatio(AspectRatioItem item)
        {
            SelectedAspectRatio = item;
        }

        [RelayCommand]
        private void StartSlide()
        {
            OpenHome(DisplayMode.Slide);
        }

        [RelayCommand]
        private void StartScroll1()
        {
            OpenHome(DisplayMode.Scroll1Row);
        }

        [RelayCommand]
        private void StartScroll4()
        {
            OpenHome(DisplayMode.Scroll4Rows);
        }

        [RelayCommand]
        private void AddImageToFolder()
        {
            var dialog = new OpenFileDialog
            {
                Title = "追加する画像を選択",
                Filter = "Image Files|*.png;*.jpg;*.jpeg",
                Multiselect = true
            };

            if (dialog.ShowDialog() != true)
                return;

            string baseFolder = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Image",
                SelectedImageFolder.ToString());

            Directory.CreateDirectory(baseFolder);

            // 現在の最大番号取得
            var existingFiles = Directory.GetFiles(baseFolder)
                .Where(f => f.EndsWith(".png") || f.EndsWith(".jpg") || f.EndsWith(".jpeg"))
                .Select(f =>
                {
                    var name = Path.GetFileNameWithoutExtension(f);
                    return int.TryParse(name, out int n) ? n : 0;
                });

            int nextNumber = existingFiles.Any() ? existingFiles.Max() + 1 : 1;

            foreach (var file in dialog.FileNames)
            {
                try
                {
                    BitmapImage bitmap = new BitmapImage(new Uri(file));

                    BitmapEncoder encoder =
                        SelectedImageSaveFormat == ImageSaveFormat.Jpeg
                            ? new JpegBitmapEncoder { QualityLevel = 90 }
                            : new PngBitmapEncoder();

                    encoder.Frames.Add(BitmapFrame.Create(bitmap));

                    string extension =
                        SelectedImageSaveFormat == ImageSaveFormat.Jpeg ? "jpg" : "png";

                    string savePath = Path.Combine(baseFolder, $"{nextNumber}.{extension}");

                    using (var fs = new FileStream(savePath, FileMode.Create))
                    {
                        encoder.Save(fs);
                    }

                    nextNumber++;
                }
                catch
                {
                    // エラー無視
                }
            }

            MessageBox.Show("画像追加完了");
        }

        [RelayCommand]
        private void DeleteImagesInFolder()
        {
            string baseFolder = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Image",
                SelectedImageFolder.ToString());

            if (!Directory.Exists(baseFolder))
            {
                MessageBox.Show("フォルダが存在しません");
                return;
            }

            var files = Directory.GetFiles(baseFolder)
                .Where(f => f.EndsWith(".png") || f.EndsWith(".jpg") || f.EndsWith(".jpeg"))
                .ToList();

            if (files.Count == 0)
            {
                MessageBox.Show("削除する画像がありません");
                return;
            }

            if (MessageBox.Show("フォルダ内の画像をすべて削除しますか？",
                    "確認",
                    MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                return;

            foreach (var file in files)
                File.Delete(file);

            MessageBox.Show("削除完了");
        }

        [RelayCommand]
        private void SelectAudio()
        {
            var dialog = new OpenFileDialog
            {
                Title = "音声ファイルを選択",
                Filter = "Audio Files|*.mp3;*.wav;*.m4a;*.wma",
                Multiselect = true
            };

            if (dialog.ShowDialog() == true)
            {
                AudioFiles.Clear();

                foreach (var file in dialog.FileNames)
                {
                    AudioFiles.Add(file);
                }

                if (AudioFiles.Count > 0)
                {
                    CurrentAudioIndex = 0;
                    CurrentAudioFileName = Path.GetFileName(AudioFiles[0]);
                }
            }
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

            var window = new Scroll1RowWindow
            {
                Width = width,
                Height = height,
                ResizeMode = ResizeMode.CanResize,   // ← リサイズ可能
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            _scrollViewModel = new Scroll1RowViewModel(
                imageHeight: height,
                aspectRatio: SelectedAspectRatio.Value,
                scrollSpeed: ScrollSpeed
            );

            window.DataContext = _scrollViewModel;
            window.Show();
        }

        private void StartScrolling()
        {
            if (_scrollViewModel == null) return;

            _scrollPosition = 0;

            _scrollTimer = new DispatcherTimer();
            _scrollTimer.Interval = TimeSpan.FromMilliseconds(30); // 固定で約33FPS
            _scrollTimer.Tick += (s, e) =>
            {
                ScrollPosition += _scrollSpeed; // スピード分ずつ進める

                // 無限スクロール
                double totalWidth = 0;
                foreach (var img in _scrollViewModel.ScrollImages)
                    totalWidth += img.Width;

                if (ScrollPosition > totalWidth / 2) // コレクションをコピーしてるので半分でリセット
                    ScrollPosition = 0;
            };
            _scrollTimer.Start();
        }

        private void OpenHome(DisplayMode mode)
        {
            var home = new HomeView
            {
                DataContext = new HomeViewModel(mode)
            };
            home.Show();
            Application.Current.Windows[0]?.Close();
        }

        public void StopScrolling()
        {
            _scrollTimer?.Stop();
        }


        partial void OnSelectedAspectRatioChanged(AspectRatioItem value)
        {
            _scrollViewModel?.UpdateSize(ImageWidth, value.Value);
        }

        partial void OnScrollSpeedChanged(double value)
        {
            _scrollViewModel?.UpdateScrollSpeed(value);
        }

        [RelayCommand]
        private void ExportPng()
        {
            SelectedFormat = ImageExportFormat.Png;
            ExportScrollVideo();
        }

        [RelayCommand]
        private void ExportJpeg()
        {
            SelectedFormat = ImageExportFormat.Jpeg;
            ExportScrollVideo();
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
        private void StopAudio()
        {
            _scrollViewModel?.StopAudio(FadeDuration);
        }



        [RelayCommand]
        private async void ExportScrollVideo()
        {
            if (_scrollViewModel == null) return;

            int fps = 30;
            int durationSeconds = ExportDurationSeconds;
            int totalFrames = fps * durationSeconds;

            string frameFolder = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "frames");

            if (Directory.Exists(frameFolder))
                Directory.Delete(frameFolder, true);

            Directory.CreateDirectory(frameFolder);

            string ffmpegPath = @"C:\Tools\ffmpeg\bin\ffmpeg.exe";
            string outputPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                $"scroll_{DateTime.Now:yyyyMMdd_HHmmss}.mp4");

            string extension = SelectedFormat == ImageExportFormat.Jpeg ? "jpg" : "png";

            BitmapEncoder CreateEncoder()
            {
                if (SelectedFormat == ImageExportFormat.Jpeg)
                    return new JpegBitmapEncoder { QualityLevel = 90 };

                return new PngBitmapEncoder();
            }

            double startScroll = _scrollViewModel.ScrollPosition;
            int frameIndex = 0;

            var scrollWindow = Application.Current.Windows
                .OfType<Scroll1RowWindow>()
                .FirstOrDefault(w => w.DataContext == _scrollViewModel);

            if (scrollWindow == null) return;

            EventHandler handler = null!;
            var tcs = new TaskCompletionSource();

            handler = (s, e) =>
            {
                if (frameIndex >= totalFrames)
                {
                    CompositionTarget.Rendering -= handler;
                    tcs.SetResult();
                    return;
                }

                _scrollViewModel.ScrollPosition +=
                    _scrollViewModel.ScrollSpeed / fps;

                double totalWidth = _scrollViewModel.ScrollImages.Sum(x => x.Width);
                if (_scrollViewModel.ScrollPosition > totalWidth / 2)
                    _scrollViewModel.ScrollPosition = 0;

                // サイズ計算
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

                // intへ変換
                int w = (int)Math.Round(width);
                int h = (int)Math.Round(height);

                // 偶数補正（超重要）
                if (w % 2 != 0) w--;
                if (h % 2 != 0) h--;

                // ウィンドウサイズを明示的に設定（重要）
                scrollWindow.Width = w;
                scrollWindow.Height = h;
                scrollWindow.UpdateLayout();

                // RenderTargetBitmapは必ずintを渡す
                var dpi = VisualTreeHelper.GetDpi(scrollWindow);

                var rtb = new RenderTargetBitmap(
                    (int)(w * dpi.DpiScaleX),
                    (int)(h * dpi.DpiScaleY),
                    96 * dpi.DpiScaleX,
                    96 * dpi.DpiScaleY,
                    PixelFormats.Pbgra32);

                rtb.Render(scrollWindow);

                rtb.Render(scrollWindow);

                var encoder = CreateEncoder();
                encoder.Frames.Add(BitmapFrame.Create(rtb));

                string filePath = Path.Combine(
                    frameFolder,
                    $"frame_{frameIndex:D4}.{extension}");

                using (var fs = new FileStream(filePath, FileMode.Create))
                    encoder.Save(fs);

                frameIndex++;
            };

            CompositionTarget.Rendering += handler;
            await tcs.Task;

            _scrollViewModel.ScrollPosition = startScroll;

            // ==========================
            // 🔥 再生中の音声位置を取得
            // ==========================
            string? audioPath = null;
            double audioStart = 0;

            if (AudioFiles.Count > 0)
            {
                audioPath = AudioFiles[CurrentAudioIndex];
                audioStart = _scrollViewModel.GetCurrentAudioPositionSeconds();
            }

            string ffmpegArgs;

            if (string.IsNullOrEmpty(audioPath))
            {
                // 音声なし（軽量設定）
                ffmpegArgs =
                    $"-y " +
                    $"-framerate {fps} " +
                    $"-i \"{frameFolder}\\frame_%04d.{extension}\" " +
                    "-c:v libx264 " +
                    "-preset slow " +        // 圧縮効率UP
                    "-crf 28 " +             // 軽量化
                    "-pix_fmt yuv420p " +
                    "-movflags +faststart " +
                    $"\"{outputPath}\"";
            }
            else
            {
                // 音声あり（再生中位置から）
                ffmpegArgs =
                    $"-y " +
                    $"-framerate {fps} " +
                    $"-i \"{frameFolder}\\frame_%04d.{extension}\" " +
                    $"-ss {audioStart:F2} " +
                    $"-i \"{audioPath}\" " +
                    "-map 0:v:0 " +
                    "-map 1:a:0 " +
                    "-c:v libx264 " +
                    "-preset slow " +
                    "-crf 28 " +
                    "-pix_fmt yuv420p " +
                    "-movflags +faststart " +
                    "-c:a aac " +
                    "-b:a 128k " +           // 音声軽量化
                    "-shortest " +
                    $"\"{outputPath}\"";
            }

            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = ffmpegArgs,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true
            };

            var process = System.Diagnostics.Process.Start(psi);
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                MessageBox.Show("FFmpegエラー:\n" + error);
                return;
            }

            MessageBox.Show($"保存完了:\n{outputPath}");
        }
        
        [RelayCommand]
        private void SelectDisplaySize(DisplaySize size)
        {
            if (SelectedDisplaySize == size)
            {
                // もう一度押したら解除
                SelectedDisplaySize = null;
                IsPresetMode = false;
                return;
            }

            SelectedDisplaySize = size;
            IsPresetMode = true;
            ImageWidth = size.Width;
        }
    }
}