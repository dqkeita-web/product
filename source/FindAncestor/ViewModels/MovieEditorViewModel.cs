using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FindAncestor.Enum;
using FindAncestor.Models;
using FindAncestor.Roc;
using FindAncestor.Services;
using FindAncestor.Views;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Channels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace FindAncestor.ViewModels
{


    public partial class MovieEditorViewModel : ObservableObject
    {
        private ID3D11Device? _d3dDevice;
        private int _engineWidth;
        private int _engineHeight;
        private ID3D11Texture2D? _renderTexture;
        private ID3D11Texture2D? _stagingTexture;

        private ID3D11DeviceContext ? _deviceContext;
        private Image? _previewImageControl;
        private D3DContext? _d3dContext;
        private FrameworkElement? _captureTarget;
        private RecordingEngine _engine = new RecordingEngine();
        private bool _isRecordingInternal;

        [ObservableProperty] private bool _isRecordingUiMode;
        [ObservableProperty] private D3DImage? _d3dImage;

        public void InitializeD3D(IntPtr hwnd, int width, int height)
        {
            _d3dContext = new D3DContext(hwnd, width, height);
        }


        private D3DContext? _d3d;
        private DispatcherTimer _renderTimer;

        private WriteableBitmap? _previewBitmap;


        private const int UI_FPS = 60;
        private int _uiFrameSkip = 0;
        private const int UI_SKIP = 3;
        private RenderTargetBitmap? _rtbCache; // RenderTargetBitmap 再利用
        private WriteableBitmap? _previewBitmapCache; // UI表示用

        private bool _isRendering;
        private const int TARGET_FPS = 60;
        private double _lastRenderTime;

        private int _recordFrameSkip = 0;
        private const int RECORD_FPS = 30;



        private readonly Channel<WriteableBitmap> _frameQueue = Channel.CreateUnbounded<WriteableBitmap>();

        private CancellationTokenSource? _cts;
        private WriteableBitmap? _reuseBitmap;

        private RenderTargetBitmap? _rtb;
        private WriteableBitmap? _wb;
        private WriteableBitmap? _uiBitmap;
        private double _recordStartTime;
        private double _currentScrollPos;
        private double _lastRecordTime;
        private double _lastSpeed;
        private double _baseTime;
        private const int FPS = 60;
        double frameInterval = 1.0 / 60.0;
        double nextFrameTime = 0;
        private VideoRenderer? _renderer;
        private ScrollingPreviewViewModel? _scrollingPreviewViewModel;
        private readonly DispatcherTimer _autoPlayTimer;
        private const double AutoScrollStep = 1.5;
        private readonly VideoExportService _videoExportService = new();
        private readonly SafeRecordingService _recordingService = new();
        private CoreWebView2Environment? _webViewEnvironment;
        [ObservableProperty] private bool _isRecording;
        private Stopwatch _uiClock = new();
        private double _lastTime;
        private double _recordStartPos;
        private Stopwatch _playClock = new();
        private WriteableBitmap? _captureBitmap;
        public event Action<string>? RecordingCompleted;
        private bool _isRenderingHooked;
        private readonly Stopwatch _recordClock = new();


        private bool _isRecordingActive;
        private byte[]? _pixelBuffer;


        public WriteableBitmap? PreviewBitmap
        {
            get => _previewBitmap;
            set => SetProperty(ref _previewBitmap, value);
        }


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

        private void StartRenderLoop()
        {
            if (_isRendering) return;

            _playClock.Restart();
            _lastRenderTime = 0;

            CompositionTarget.Rendering += OnRendering;
            _isRendering = true;
        }
        public void InitializeRecordingTarget(FrameworkElement target)
        {
            _captureTarget = target;
        }
        public void InitializeD3DPreview(Image image)
        {
            _d3dImage = new D3DImage();
            image.Source = _d3dImage;
        }   
        private DispatcherTimer _recordTimer;

        public async Task StopRecordingAsync()
        {
            if (!IsRecording) return;

            CompositionTarget.Rendering -= OnRenderFrame;

            // 🔥 これも必須
            _isRecordingInternal = false;

            await _engine.StopAsync();

            IsRecording = false;
            IsRecordingUiMode = false;

            _engineWidth = 0;
            _engineHeight = 0;
        }
        private void OnRenderingCapture(object sender, EventArgs e)
        {
            if (!IsRecording) return;
            if (_captureTarget == null) return;

            int width = (int)_captureTarget.ActualWidth;
            int height = (int)_captureTarget.ActualHeight;

            if (width <= 0 || height <= 0) return;

            var rtb = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(_captureTarget);

            var wb = new WriteableBitmap(rtb);
            wb.Freeze();

            _engine.EnqueueFrame(wb);

            Debug.WriteLine($"FRAME送信 {width}x{height}");
        }
        private async Task ProcessFrameQueueAsync(CancellationToken token)
        {
            try
            {
                await foreach (var frame in _frameQueue.Reader.ReadAllAsync(token))
                {
                    _engine.EnqueueFrame(frame);
                }
            }
            catch (OperationCanceledException)
            {
                // 正常終了
            }
        }


        public void StartRecording(FrameworkElement captureTarget, string path, int width, int height)
        {
            if (IsRecording) return;

            _captureTarget = captureTarget;

            _engineWidth = width;
            _engineHeight = height;

            IsRecording = true;
            IsRecordingUiMode = true;

            // 🔥 これが抜けてる（致命的）
            _isRecordingInternal = true;

            _engine.Start(path, width, height);

            CompositionTarget.Rendering += OnRenderFrame;
        }


        private void StopRenderLoop()
        {
            if (!_isRendering) return;

            CompositionTarget.Rendering -= OnRendering;
            _isRendering = false;
        }

        private void OnRenderFrame(object? sender, EventArgs e)
        {
            if (!_isRecordingInternal || _captureTarget == null) return;

            int width = _engineWidth;   // Start時の値と一致させる
            int height = _engineHeight;
            if (width <= 0 || height <= 0) return;

            width = width / 2 * 2;
            height = height / 2 * 2;

            // 🔥 再利用
            if (_rtb == null ||
                _rtb.PixelWidth != width ||
                _rtb.PixelHeight != height)
            {
                _rtb = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            }

            var dv = new DrawingVisual();

            using (var dc = dv.RenderOpen())
            {
                // 🔥 ズレ修正（最重要）
                var vb = new VisualBrush(_captureTarget)
                {
                    Stretch = Stretch.Fill,
                    AlignmentX = AlignmentX.Left,
                    AlignmentY = AlignmentY.Top
                };

                dc.DrawRectangle(vb, null, new Rect(0, 0, width, height));
            }

            _rtb.Clear();
            _rtb.Render(dv);

            var wb = new WriteableBitmap(_rtb);
            wb.Freeze();

            _engine.EnqueueFrame(wb);

            _uiFrameSkip++;
            if (_uiFrameSkip >= UI_SKIP)
            {
                _uiFrameSkip = 0;
                PreviewBitmap = wb;
            }
        }


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
            _engine.RecordingCompleted += path => { RecordingCompleted?.Invoke(path); };

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
            try
            {
                _engine.Stop();
            }
            catch
            {
            }

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
        private void CreateStagingTexture(int width, int height)
        {
            if (_d3dDevice == null) return;

            var desc = new Texture2DDescription
            {
                Width = (uint)width,
                Height = (uint)height,
                MipLevels = 1,
                ArraySize = 1,
                Format = Format.B8G8R8A8_UNorm,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Staging,
                BindFlags = BindFlags.None
            };

            _stagingTexture = _d3dDevice.CreateTexture2D(desc);
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
            var vm = _scrollingPreviewViewModel ?? EmbeddedPreviewViewModel;
            if (vm == null) return;

            _recordStartPos = vm.ScrollPosition;

            _playClock.Restart();

            if (!_autoPlayTimer.IsEnabled)
                _autoPlayTimer.Start();
        }



        private void StopAutoPlay()
        {
            if (_autoPlayTimer.IsEnabled) _autoPlayTimer.Stop();
        }

        private Stopwatch _clock = Stopwatch.StartNew();

        private void AutoPlayTick(object? sender, EventArgs e)
        {
            var vm = _scrollingPreviewViewModel ?? EmbeddedPreviewViewModel;
            if (vm == null || _renderer == null) return;

            double now = _playClock.Elapsed.TotalSeconds;

            double delta = now - _recordStartTime;
            _recordStartTime = now;

            _currentScrollPos += ScrollSpeed * delta;

            vm.ScrollPosition = _currentScrollPos;

            var bmp = _renderer.Render(_currentScrollPos);
            bmp.Freeze();


        }

        private WriteableBitmap? _recordWb;

        private void UpdatePreviewBitmap(BitmapSource src)
        {
            if (_previewBitmap == null ||
                _previewBitmap.PixelWidth != src.PixelWidth ||
                _previewBitmap.PixelHeight != src.PixelHeight)
            {
                _previewBitmap = new WriteableBitmap(src);
                OnPropertyChanged(nameof(PreviewBitmap));
                return;
            }

            _previewBitmap.Lock();

            src.CopyPixels(
                new Int32Rect(0, 0, src.PixelWidth, src.PixelHeight),
                _previewBitmap.BackBuffer,
                _previewBitmap.BackBufferStride * _previewBitmap.PixelHeight,
                _previewBitmap.BackBufferStride);

            _previewBitmap.AddDirtyRect(new Int32Rect(0, 0, src.PixelWidth, src.PixelHeight));
            _previewBitmap.Unlock();
        }

        private RenderTargetBitmap? _recordRtb;


        public void ApplyImageWidth()
        {
            UpdatePreviewSize(ImageWidth, SelectedAspectRatio.Value);
            LoadImagesFromFolder(SelectedPlayFolder);
        }

        [RelayCommand]
        private async Task ToggleRecording()
        {
            if (IsRecording)
            {
                await StopRecordingAsync();
            }
            else
            {
                StartRecordingInternal();
            }
        }

        private void StartRecordingInternal()
        {
            // MainWindow から RootGrid を取得
            var view = Application.Current.MainWindow as Views.MovieEditorView;
            if (view == null) return;

            var captureTarget = view.RootGrid;

            // 幅・高さを偶数に丸める
            int width = (int)captureTarget.ActualWidth / 2 * 2;
            int height = (int)captureTarget.ActualHeight / 2 * 2;

            string folder = @"E:\ffmpegMovie";
            Directory.CreateDirectory(folder);

            string path = Path.Combine(folder, $"video_{DateTime.Now:HHmmss}.mp4");

            // 本来の録画開始処理
            StartRecording(captureTarget, path, width, height);
        }

        [ObservableProperty] private double _scrollPosition;

        partial void OnScrollPositionChanged(double value)
        {
            _scrollingPreviewViewModel?.ScrollPosition = value;
            EmbeddedPreviewViewModel?.ScrollPosition = value;
        }

        private void OnRendering(object? sender, EventArgs e)
        {
            var vm = _scrollingPreviewViewModel ?? EmbeddedPreviewViewModel;
            if (vm == null || _renderer == null) return;

            double t = _playClock.Elapsed.TotalSeconds;

            double scrollPos = _recordStartPos + ScrollSpeed * t;
            vm.ScrollPosition = scrollPos;

            var bmp = _renderer.Render(scrollPos);
            bmp.Freeze();


            _uiFrameSkip++;
            if (_uiFrameSkip >= UI_SKIP)
            {
                _uiFrameSkip = 0;

                if (_previewBitmapCache == null ||
                    _previewBitmapCache.PixelWidth != bmp.PixelWidth ||
                    _previewBitmapCache.PixelHeight != bmp.PixelHeight)
                {
                    _previewBitmapCache = new WriteableBitmap(bmp);
                    PreviewBitmap = _previewBitmapCache;
                }
                else
                {
                    _previewBitmapCache.Lock();

                    bmp.CopyPixels(
                        new Int32Rect(0, 0, bmp.PixelWidth, bmp.PixelHeight),
                        _previewBitmapCache.BackBuffer,
                        _previewBitmapCache.BackBufferStride * bmp.PixelHeight,
                        _previewBitmapCache.BackBufferStride);

                    _previewBitmapCache.AddDirtyRect(
                        new Int32Rect(0, 0, bmp.PixelWidth, bmp.PixelHeight));

                    _previewBitmapCache.Unlock();
                }
            }

            if (_isRecordingInternal)
            {
                _engine.EnqueueFrame(bmp);
            }


        }

    }
}

