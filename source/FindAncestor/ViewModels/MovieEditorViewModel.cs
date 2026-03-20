using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FindAncestor.Enum;
using FindAncestor.Models;
using FindAncestor.Roc;
using FindAncestor.Roc;
using FindAncestor.Services;
using FindAncestor.Views;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace FindAncestor.ViewModels
{
    public partial class MovieEditorViewModel : ObservableObject
    {
        private ScrollingPreviewViewModel? _scrollingPreviewViewModel;
        private readonly DispatcherTimer _autoPlayTimer;
        private const double AutoScrollStep = 1.5;
        private readonly VideoExportService _videoExportService = new();
        private readonly SafeRecordingService _recordingService = new();
        private CoreWebView2Environment? _webViewEnvironment;
        [ObservableProperty]
        private bool _isRecording;
        private Stopwatch _uiClock = new();
        private double _lastTime;
        private readonly RecordingEngine _engine = new();
        public WebView2 ExternalWebView { get; set; }
        [ObservableProperty] private FolderPanelMode _currentPanelMode = FolderPanelMode.None;
        [ObservableProperty] private ImageFolderType _selectedPlayFolder = ImageFolderType.A;
        [ObservableProperty] private DisplaySize? _selectedDisplaySize;
        [ObservableProperty] private bool _isPresetMode;
        [ObservableProperty] private AspectRatioItem _selectedAspectRatio = new AspectRatioItem("16:9", 16.0 / 9.0);
        [ObservableProperty] private double _imageWidth = 900;
        [ObservableProperty] private double _scrollSpeed = 3;
        [ObservableProperty] private double _uiOpacity = 1;
        [ObservableProperty] private double _settingPanelX = 0;
        [ObservableProperty] private bool _isPlaying;
        [ObservableProperty] private ObservableCollection<string> _audioFiles = [];
        [ObservableProperty] private int _currentAudioIndex;
        [ObservableProperty] private bool _isLoopEnabled = true;
        [ObservableProperty] private double _fadeDuration = 1.5;
        [ObservableProperty] private bool _isEmbeddedMode;
        [ObservableProperty] private ScrollingPreviewViewModel? _embeddedPreviewViewModel;
        [ObservableProperty] private ImageFolderType _selectedFolder = ImageFolderType.A;
        [ObservableProperty] private ObservableCollection<FolderPreviewItem> _folderPreviews = new();
        [ObservableProperty] private double _volume = 0.5;
        [ObservableProperty] private bool _isExternalVideoVisible;
        [ObservableProperty] private SliderPanelMode _currentSliderMode = SliderPanelMode.None;

        public ObservableCollection<DisplaySize> DisplaySizes { get; } =
        [
            new DisplaySize { Name = "HD", Width = 1280 },
            new DisplaySize { Name = "FullHD", Width = 1920, Height = 1080 },
            new DisplaySize { Name = "2K", Width = 2560, Height = 1440 },
            new DisplaySize { Name = "4K", Width = 3840, Height = 2160 }
        ];

        public ObservableCollection<AspectRatioItem> AspectRatios { get; } =
        [
            new AspectRatioItem("16:9", 16.0 / 9.0), new AspectRatioItem("4:3", 4.0 / 3.0),
            new AspectRatioItem("1:1", 1.0), new AspectRatioItem("3:2", 3.0 / 2.0)
        ];

        public RangeObservableCollection<ImageWithWidth> ScrollImages { get; } = new();

        public MovieEditorViewModel()
        {
            if (AspectRatios.Count > 0) SelectedAspectRatio = AspectRatios[0];
            _autoPlayTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16)
            };
            _autoPlayTimer.Tick += AutoPlayTick;
            LoadFolderPreviews();
        }

        [RelayCommand]
        private void ToggleAddPanel()
        {
            CurrentPanelMode = CurrentPanelMode == FolderPanelMode.Add ? FolderPanelMode.None : FolderPanelMode.Add;
        }

        [RelayCommand]
        private void ToggleDeletePanel()
        {
            CurrentPanelMode = CurrentPanelMode == FolderPanelMode.Delete
                ? FolderPanelMode.None
                : FolderPanelMode.Delete;
        }

        [RelayCommand]
        private void TogglePlayPanel()
        {
            CurrentPanelMode = CurrentPanelMode == FolderPanelMode.Play ? FolderPanelMode.None : FolderPanelMode.Play;
        }

        [RelayCommand]
        private void SelectPlayFolder(ImageFolderType folder)
        {
            SelectedPlayFolder = folder;
            if (_scrollingPreviewViewModel == null && EmbeddedPreviewViewModel == null) OpenEmbeddedScroll();
            LoadImagesFromFolder(folder);
        }

        private async void LoadImagesFromFolder(ImageFolderType folder)
        {

            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Image", folder.ToString());
            if (!Directory.Exists(path)) return;
            var target = _scrollingPreviewViewModel ?? EmbeddedPreviewViewModel;
            if (target == null) return;
            target.ScrollImages.Clear();
            var files = Directory.GetFiles(path)
                .Where(f => f.EndsWith(".png") || f.EndsWith(".jpg") || f.EndsWith(".jpeg")).ToArray();
            var items = await Task.Run(() =>
            {
                var list = new List<ImageWithWidth>();
                foreach (var file in files)
                {
                    var bitmap = LoadImage(file);
                    double height = ImageWidth / SelectedAspectRatio.Value;
                    double width = bitmap.PixelWidth * (height / bitmap.PixelHeight);
                    list.Add(new ImageWithWidth { Source = bitmap, Width = width, Height = height });
                }

                list.AddRange(list);
                return list;
            });
            foreach (var item in items) target.ScrollImages.Add(item);
        }

        private BitmapImage LoadImage(string path)
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();
            return image;
        }

        [RelayCommand]
        private void CloseApplication()
        {
            Application.Current.Shutdown();
        }

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

        private void UpdatePreviewSize(double width, double aspect)
        {
            _scrollingPreviewViewModel?.UpdateSize(width, aspect);
            EmbeddedPreviewViewModel?.UpdateSize(width, aspect);
        }

        private void UpdateScrollSpeedAll(double speed)
        {
            _scrollingPreviewViewModel?.UpdateScrollSpeed(speed);
            EmbeddedPreviewViewModel?.UpdateScrollSpeed(speed);
        }

        partial void OnScrollSpeedChanged(double value)
        {
            UpdateScrollSpeedAll(value);
        }

        partial void OnSelectedAspectRatioChanged(AspectRatioItem value)
        {
            UpdatePreviewSize(ImageWidth, value.Value);
        }

        [RelayCommand]
        private void SelectAudio()
        {
            var dialog = new OpenFileDialog { Filter = "Audio Files|*.mp3;*.wav", Multiselect = true };
            if (dialog.ShowDialog() == true)
            {
                AudioFiles.Clear();
                foreach (var file in dialog.FileNames) AudioFiles.Add(file);
                CurrentAudioIndex = 0;
            }
        }

        private void StartAudioInternal()
        {
            if (EmbeddedPreviewViewModel == null || AudioFiles.Count == 0) return;
            EmbeddedPreviewViewModel.StartAudio(AudioFiles[CurrentAudioIndex], IsLoopEnabled, FadeDuration);
        }

        private void StopAudioInternal()
        {
            EmbeddedPreviewViewModel?.StopAudio(FadeDuration);
        }

        [RelayCommand]
        private void TogglePlay()
        {
            if (IsPlaying)
            {
                StopAudioInternal();
                IsPlaying = false;
            }
            else
            {
                StartAudioInternal();
                IsPlaying = true;
            }
        }

        [RelayCommand]
        private void NextAudio()
        {
            if (AudioFiles.Count == 0) return;
            CurrentAudioIndex = (CurrentAudioIndex + 1) % AudioFiles.Count;
            StartAudioInternal();
        }

        [RelayCommand]
        private void PreviousAudio()
        {
            if (AudioFiles.Count == 0) return;
            CurrentAudioIndex--;
            if (CurrentAudioIndex < 0) CurrentAudioIndex = AudioFiles.Count - 1;
            StartAudioInternal();
        }

        private void CreatePreviewSize(out double width, out double height)
        {
            if (IsPresetMode && SelectedDisplaySize != null)
            {
                width = SelectedDisplaySize.Width;
                height = SelectedDisplaySize.Height;
                return;
            }

            // 🔥 null対策
            var aspect = SelectedAspectRatio?.Value ?? (16.0 / 9.0);

            width = ImageWidth;
            height = ImageWidth / aspect;
        }

        [RelayCommand]
        private void OpenSingleRowScroll()
        {
            CreatePreviewSize(out var width, out var height);
            _scrollingPreviewViewModel = new ScrollingPreviewViewModel(height, SelectedAspectRatio.Value, ScrollSpeed);
            var window = new ShowMovie
            {
                Width = width, Height = height, WindowStartupLocation = WindowStartupLocation.CenterScreen,
                DataContext = _scrollingPreviewViewModel
            };
            window.Show();
            StartAutoPlay();
        }

        [RelayCommand]
        private void OpenEmbeddedScroll()
        {
            CreatePreviewSize(out var width, out var height);
            EmbeddedPreviewViewModel = new ScrollingPreviewViewModel(height, SelectedAspectRatio.Value, ScrollSpeed);
            IsEmbeddedMode = true;
            StartAutoPlay();
        }

        [RelayCommand]
        private void CloseEmbedded()
        {
            IsEmbeddedMode = false;
            EmbeddedPreviewViewModel = null;
            GC.Collect();
        }

        [RelayCommand]
        private void AddImageToFolder()
        {
        }

        public void AddImage(string path)
        {
            if (_scrollingPreviewViewModel == null && EmbeddedPreviewViewModel == null) OpenEmbeddedScroll();
            var target = _scrollingPreviewViewModel ?? EmbeddedPreviewViewModel;
            if (target == null) return;
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(path);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();
            double height = ImageWidth / SelectedAspectRatio.Value;
            double width = bitmap.PixelWidth * (height / bitmap.PixelHeight);
            target.ScrollImages.Add(new ImageWithWidth { Source = bitmap, Width = width, Height = height });
        }

        [RelayCommand]
        private void SelectFolderAndAddImages(ImageFolderType folder)
        {
            var dialog = new OpenFileDialog { Filter = "Image Files|*.png;*.jpg;*.jpeg", Multiselect = true };
            if (dialog.ShowDialog() != true) return;
            ImageStorageService.SaveImages(dialog.FileNames, folder, ImageSaveFormat.Jpeg);
            SelectedPlayFolder = folder;
            if (_scrollingPreviewViewModel == null && EmbeddedPreviewViewModel == null) OpenEmbeddedScroll();
            LoadImagesFromFolder(folder);
            LoadFolderPreviews();
        }

        [RelayCommand]
        private void DeleteImagesInFolder()
        {
            if (_scrollingPreviewViewModel != null) _scrollingPreviewViewModel.ScrollImages.Clear();
            if (EmbeddedPreviewViewModel != null) EmbeddedPreviewViewModel.ScrollImages.Clear();
        }

        [RelayCommand]
        private void ToggleVolumeSlider()
        {
            CurrentSliderMode = CurrentSliderMode == SliderPanelMode.Volume
                ? SliderPanelMode.None
                : SliderPanelMode.Volume;
        }

        [RelayCommand]
        private void ToggleSpeedSlider()
        {
            CurrentSliderMode = CurrentSliderMode == SliderPanelMode.Speed
                ? SliderPanelMode.None
                : SliderPanelMode.Speed;
        }

        [RelayCommand]
        private void ToggleSizeSlider()
        {
            CurrentSliderMode = CurrentSliderMode == SliderPanelMode.Size ? SliderPanelMode.None : SliderPanelMode.Size;
        }

        private void LoadFolderPreviews()
        {
            FolderPreviews.Clear();
            var folders = new[]
            {
                (ImageFolderType.A, "#FF5555"), (ImageFolderType.B, "#55FF55"), (ImageFolderType.C, "#5599FF"),
                (ImageFolderType.D, "#FFCC55")
            };
            foreach (var (folder, color) in folders)
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Image", folder.ToString());
                var firstImage = Directory.Exists(path) ? Directory.GetFiles(path).FirstOrDefault() : null;
                BitmapImage? thumb = firstImage != null ? LoadImage(firstImage) : null;
                FolderPreviews.Add(new FolderPreviewItem { Folder = folder, Thumbnail = thumb, Color = color });
            }
        }

        partial void OnVolumeChanged(double value)
        {
            _scrollingPreviewViewModel?.SetVolume(value);
            EmbeddedPreviewViewModel?.SetVolume(value);
        }

        [RelayCommand]
        private void DeleteFolderImages(ImageFolderType folder)
        {
            ImageStorageService.DeleteImages(folder);
            _scrollingPreviewViewModel?.ScrollImages.Clear();
            EmbeddedPreviewViewModel?.ScrollImages.Clear();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            LoadFolderPreviews();
        }

        private void StartAutoPlay()
        {
            if (!_autoPlayTimer.IsEnabled) _autoPlayTimer.Start();
        }

        private void StopAutoPlay()
        {
            if (_autoPlayTimer.IsEnabled) _autoPlayTimer.Stop();
        }

        [RelayCommand]
        private void StartRecording()
        {
            CreatePreviewSize(out var w, out var h);

            string folder = @"E:\ffmpegMovie";
            Directory.CreateDirectory(folder);

            string path = Path.Combine(folder, $"video_{DateTime.Now:HHmmss}.mp4");

            var vm = _scrollingPreviewViewModel ?? EmbeddedPreviewViewModel;
            if (vm == null) return;

            var model = new ScrollRenderModel();

            foreach (var img in vm.ScrollImages)
            {
                if (img.Source is BitmapSource bmp)
                {
                    model.Images.Add(bmp);
                    model.Widths.Add(img.Width);
                    model.Heights.Add(img.Height);
                }
            }

            _engine.Start(path, (int)w, (int)h, model);
        }
        [RelayCommand]
        private void StopRecording()
        {
            _engine.Stop();
        }

        private void AutoPlayTick(object? sender, EventArgs e)
        {
            var vm = _scrollingPreviewViewModel ?? EmbeddedPreviewViewModel;
            if (vm == null) return;

            vm.ScrollPosition += ScrollSpeed;

            // ★録画（ここだけ）
            _engine.ProcessFrame(vm);
        }
        private RenderTargetBitmap CapturePreview()
        {
            var element = Application.Current.MainWindow;

            int width = (int)element.ActualWidth;
            int height = (int)element.ActualHeight;

            // ★偶数化（FFmpeg対策）
            width = width / 2 * 2;
            height = height / 2 * 2;

            var rtb = new RenderTargetBitmap(
                width,
                height,
                96,
                96,
                PixelFormats.Pbgra32);

            rtb.Render(element);

            return rtb;
        }
        public void ApplyImageWidth()
        {
            UpdatePreviewSize(ImageWidth, SelectedAspectRatio.Value);
            LoadImagesFromFolder(SelectedPlayFolder);
        }


        [RelayCommand]
        private void ToggleRecording()
        {
            if (IsRecording)
            {
                StopRecording();
                IsRecording = false;
            }
            else
            {
                StartRecording();
                IsRecording = true;
            }
        }
        [ObservableProperty] private double _scrollPosition;

        partial void OnScrollPositionChanged(double value)
        {
            _scrollingPreviewViewModel?.ScrollPosition = value;
            EmbeddedPreviewViewModel?.ScrollPosition = value;
        }
    }
}