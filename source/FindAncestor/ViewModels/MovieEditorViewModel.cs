    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;
    using FindAncestor.Enum;
    using FindAncestor.Models;
    using FindAncestor.Roc;
    using FindAncestor.Services;
    using FindAncestor.WinRoc;
    using Microsoft.Win32;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.IO;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Threading;
    using WinRocRecorder = FindAncestor.WinRoc.WinRocRecorder;

    namespace FindAncestor.ViewModels
    {
        public partial class MovieEditorViewModel : ObservableObject
        {
            [ObservableProperty] private bool _isRecording;
            [ObservableProperty] private bool _isRecordingUiMode;
            [ObservableProperty] private bool _isPlaying;
            [ObservableProperty] private bool _isEmbeddedMode;
            [ObservableProperty] private Rect? _selectedRegion;
            [ObservableProperty] private double _imageWidth = 900;
            [ObservableProperty] private double _scrollSpeed = 3;
            [ObservableProperty] private double _uiOpacity = 1;

            [ObservableProperty] private AspectRatioItem _selectedAspectRatio = new("16:9", 16.0 / 9.0);

            [ObservableProperty] private ImageFolderType _selectedPlayFolder = ImageFolderType.A;
            [ObservableProperty] private DisplaySize? _selectedDisplaySize;
            [ObservableProperty] private bool _isPresetMode;

            [ObservableProperty] private FolderPanelMode _currentPanelMode = FolderPanelMode.None;
            [ObservableProperty] private SliderPanelMode _currentSliderMode = SliderPanelMode.None;

            [ObservableProperty] private ObservableCollection<string> _audioFiles = new();
            [ObservableProperty] private int _currentAudioIndex;
            [ObservableProperty] private bool _isLoopEnabled = true;
            [ObservableProperty] private double _fadeDuration = 1.5;

            [ObservableProperty] private ObservableCollection<FolderPreviewItem> _folderPreviews = new();
            [ObservableProperty] private double _volume = 0.5;

            [ObservableProperty] private ScrollingPreviewViewModel? _embeddedPreviewViewModel;
            [ObservableProperty] private bool _isWinRocRecording;
            [ObservableProperty]
            private bool _isRegionSelecting;
            private WinRoc.WinRocRecorder _winRoc;
            private readonly RecordingEngine _engine = new();
            private ScrollingPreviewViewModel? _scrollingPreviewViewModel;
            private WinRocRegion? _lastSelectedRegion;
            private FrameworkElement? _captureTarget;

            public event Action<string>? RecordingCompleted;
            private int _engineWidth;
            private int _engineHeight;
            private readonly DispatcherTimer _autoPlayTimer;
            private readonly Stopwatch _playClock = new();
            private double _currentScrollPos;
            private double _recordStartTime;
            private bool _isRecordingInternal;
            private RenderTargetBitmap? _rtb;
            public MovieEditorViewModel()
            {
                _engine.RecordingCompleted += path =>
                {
                    RecordingCompleted?.Invoke(path);
                };

                _winRoc = new WinRocRecorder(_engine); 

                _autoPlayTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(16)
                };
                _autoPlayTimer.Tick += AutoPlayTick;

                LoadFolderPreviews();
            
            }

            public void AddImage(string path)
            {
                if (_scrollingPreviewViewModel == null && EmbeddedPreviewViewModel == null)
                    OpenEmbeddedScroll();

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

                target.ScrollImages.Add(new ImageWithWidth
                {
                    Source = bitmap,
                    Width = width,
                    Height = height
                });
            }

            public void ApplyImageWidth()
            {
                UpdatePreviewSize(ImageWidth, SelectedAspectRatio.Value);
                LoadImagesFromFolder(SelectedPlayFolder);
            }

            private async void LoadImagesFromFolder(ImageFolderType folder)
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Image", folder.ToString());
                if (!Directory.Exists(path)) return;

                var target = _scrollingPreviewViewModel ?? EmbeddedPreviewViewModel;
                if (target == null) return;

                target.ScrollImages.Clear();

                var files = Directory.GetFiles(path)
                    .Where(f => f.EndsWith(".png") || f.EndsWith(".jpg") || f.EndsWith(".jpeg"))
                    .ToArray();

                foreach (var file in files)
                {
                    AddImage(file);
                }
            }

            private BitmapImage LoadImage(string path)
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(path);
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                bmp.Freeze();
                return bmp;
            }

            private void UpdatePreviewSize(double width, double aspect)
            {
                _scrollingPreviewViewModel?.UpdateSize(width, aspect);
                EmbeddedPreviewViewModel?.UpdateSize(width, aspect);
            }

            partial void OnScrollSpeedChanged(double value)
            {
                _scrollingPreviewViewModel?.UpdateScrollSpeed(value);
                EmbeddedPreviewViewModel?.UpdateScrollSpeed(value);
            }

            partial void OnSelectedAspectRatioChanged(AspectRatioItem value)
            {
                UpdatePreviewSize(ImageWidth, value.Value);
            }

            partial void OnVolumeChanged(double value)
            {
                _scrollingPreviewViewModel?.SetVolume(value);
                EmbeddedPreviewViewModel?.SetVolume(value);
            }

            [RelayCommand]
            private void OpenEmbeddedScroll()
            {
                double height = ImageWidth / SelectedAspectRatio.Value;
                EmbeddedPreviewViewModel =
                    new ScrollingPreviewViewModel(height, SelectedAspectRatio.Value, ScrollSpeed);

                IsEmbeddedMode = true;
                StartAutoPlay();
            }

            [RelayCommand]
            private void CloseEmbedded()
            {
                IsEmbeddedMode = false;
                EmbeddedPreviewViewModel = null;
            }

            [RelayCommand]
            private void SelectPlayFolder(ImageFolderType folder)
            {
                SelectedPlayFolder = folder;

                if (_scrollingPreviewViewModel == null && EmbeddedPreviewViewModel == null)
                    OpenEmbeddedScroll();

                LoadImagesFromFolder(folder);
                StartAutoPlay();
            }

            [RelayCommand]
            private void SelectFolderAndAddImages(ImageFolderType folder)
            {
                var dialog = new OpenFileDialog
                {
                    Filter = "Image Files|*.png;*.jpg;*.jpeg",
                    Multiselect = true
                };

                if (dialog.ShowDialog() != true) return;

                ImageStorageService.SaveImages(dialog.FileNames, folder, ImageSaveFormat.Jpeg);

                SelectedPlayFolder = folder;

                if (_scrollingPreviewViewModel == null && EmbeddedPreviewViewModel == null)
                    OpenEmbeddedScroll();

                LoadImagesFromFolder(folder);
                LoadFolderPreviews();
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

            [RelayCommand]
            private void ToggleAddPanel()
                => CurrentPanelMode = CurrentPanelMode == FolderPanelMode.Add ? FolderPanelMode.None : FolderPanelMode.Add;

            [RelayCommand]
            private void ToggleDeletePanel()
                => CurrentPanelMode = CurrentPanelMode == FolderPanelMode.Delete ? FolderPanelMode.None : FolderPanelMode.Delete;

            [RelayCommand]
            private void TogglePlayPanel()
                => CurrentPanelMode = CurrentPanelMode == FolderPanelMode.Play ? FolderPanelMode.None : FolderPanelMode.Play;

            [RelayCommand]
            private void ToggleVolumeSlider()
                => CurrentSliderMode = CurrentSliderMode == SliderPanelMode.Volume ? SliderPanelMode.None : SliderPanelMode.Volume;

            [RelayCommand]
            private void ToggleSpeedSlider()
                => CurrentSliderMode = CurrentSliderMode == SliderPanelMode.Speed ? SliderPanelMode.None : SliderPanelMode.Speed;

            [RelayCommand]
            private void ToggleSizeSlider()
                => CurrentSliderMode = CurrentSliderMode == SliderPanelMode.Size ? SliderPanelMode.None : SliderPanelMode.Size;

            private void StartAudioInternal()
            {
                var target = _scrollingPreviewViewModel ?? EmbeddedPreviewViewModel;
                if (target == null || AudioFiles.Count == 0) return;

                target.StartAudio(AudioFiles[CurrentAudioIndex], IsLoopEnabled, FadeDuration);
            }

            private void StopAudioInternal()
            {
                var target = _scrollingPreviewViewModel ?? EmbeddedPreviewViewModel;
                target?.StopAudio(FadeDuration);
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
            public void ToggleRegionEdit()
            {
                IsEditingRegion = !IsEditingRegion;

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
                if (CurrentAudioIndex < 0)
                    CurrentAudioIndex = AudioFiles.Count - 1;

                StartAudioInternal();
            }

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

            [RelayCommand]
            private void CloseApplication()
            {
                try { _engine.Stop(); } catch { }
                Application.Current.Shutdown();
            }

            private void LoadFolderPreviews()
            {
                FolderPreviews.Clear();

                var folders = new[]
                {
                    (ImageFolderType.A, "#FF5555"),
                    (ImageFolderType.B, "#55FF55"),
                    (ImageFolderType.C, "#5599FF"),
                    (ImageFolderType.D, "#FFCC55")
                };

                foreach (var (folder, color) in folders)
                {
                    string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Image", folder.ToString());

                    var firstImage = Directory.Exists(path)
                        ? Directory.GetFiles(path).FirstOrDefault()
                        : null;

                    BitmapImage? thumb = firstImage != null ? LoadImage(firstImage) : null;

                    FolderPreviews.Add(new FolderPreviewItem
                    {
                        Folder = folder,
                        Thumbnail = thumb,
                        Color = color
                    });
                }
            }

            private void StartAutoPlay()
            {
                var vm = _scrollingPreviewViewModel ?? EmbeddedPreviewViewModel;
                if (vm == null) return;

                _currentScrollPos = vm.ScrollPosition;
                _playClock.Restart();

                if (!_autoPlayTimer.IsEnabled)
                    _autoPlayTimer.Start();
            }

            private void AutoPlayTick(object? sender, EventArgs e)
            {
                var vm = _scrollingPreviewViewModel ?? EmbeddedPreviewViewModel;
                if (vm == null) return;

                double now = _playClock.Elapsed.TotalSeconds;
                double delta = now - _recordStartTime;
                _recordStartTime = now;

                _currentScrollPos += ScrollSpeed * delta;
                vm.ScrollPosition = _currentScrollPos;
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
            [RelayCommand]
            private async Task ToggleWinRocRecording()
            {
                if (IsWinRocRecording)
                {
                    await StopWinRocAsync();
                }
                else
                {
                    await StartWinRocRecording();
                }
            }

            private string CreatePath()
            {
                string folder = @"E:\ffmpegMovie";
                Directory.CreateDirectory(folder);

                return Path.Combine(folder, $"winroc_{DateTime.Now:HHmmss}.mp4");
            }

            private void StartRecordingInternal()
            {
                var view = Application.Current.MainWindow as Views.MovieEditorView;
                if (view == null) return;

                var captureTarget = view.RootGrid;

                int width = (int)captureTarget.ActualWidth / 2 * 2;
                int height = (int)captureTarget.ActualHeight / 2 * 2;

                string folder = @"E:\ffmpegMovie";
                Directory.CreateDirectory(folder);

                string path = Path.Combine(folder, $"video_{DateTime.Now:HHmmss}.mp4");

                StartRecording(captureTarget, path, width, height);
            }

            public void StartRecording(FrameworkElement captureTarget, string path, int width, int height)
            {
                if (IsRecording) return;

                _captureTarget = captureTarget;

                _engineWidth = width;
                _engineHeight = height;

                IsRecording = true;
                IsRecordingUiMode = true;

                _isRecordingInternal = true;

                _engine.Start(path, width, height);

                CompositionTarget.Rendering += OnRenderFrame;
            }

            public async Task StopRecordingAsync()
            {
                if (!IsRecording) return;

                CompositionTarget.Rendering -= OnRenderFrame;

                _isRecordingInternal = false;

                await _engine.StopAsync();

                IsRecording = false;
                IsRecordingUiMode = false;

                _engineWidth = 0;
                _engineHeight = 0;
            }

            private void OnRenderFrame(object? sender, EventArgs e)
            {
                if (!_isRecordingInternal || _captureTarget == null) return;

                int width = _engineWidth;
                int height = _engineHeight;

                if (width <= 0 || height <= 0) return;

                width = width / 2 * 2;
                height = height / 2 * 2;

                if (_rtb == null || _rtb.PixelWidth != width || _rtb.PixelHeight != height)
                {
                    _rtb = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
                }

                var dv = new DrawingVisual();
                using (var dc = dv.RenderOpen())
                {
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
            }

            [ObservableProperty]
            private bool _isSelectingRegion;

            [ObservableProperty]
            private bool _isEditingRegion;

            [RelayCommand]
            public void StartRegionSelect()
            {
                if (Application.Current.MainWindow is not Views.MovieEditorView v) return;

                v.StartRegionSelect();
            }
            public void OnRegionSelected(Rect rect)
            {
                SelectedRegion = rect;

                if (Application.Current.MainWindow is Views.MovieEditorView view)
                {
                    view.ShowRecordingBorder(rect);

                    // 🔥ここが超重要
                    var screenTopLeft = view.PointToScreen(new Point(rect.X, rect.Y));

                    _lastSelectedRegion = new WinRocRegion
                    {
                        X = (int)screenTopLeft.X,
                        Y = (int)screenTopLeft.Y,
                        Width = (int)rect.Width,
                        Height = (int)rect.Height
                    };
            }
            }

        [RelayCommand]
            private async Task StartWinRocRecording()
            {
                if (_lastSelectedRegion == null)
                {
                    Debug.WriteLine("❌ 範囲未選択");
                    return;
                }

                var region = _lastSelectedRegion.Value;

                if (Application.Current.MainWindow is not Views.MovieEditorView view)
                    return;

                string path = CreatePath();

                view.HideRecordingBorder();
                view.ShowRec();

                IsWinRocRecording = true;
                IsRecordingUiMode = true;

                await _winRoc.StartRecordingAsync(path, region);
            }
            private async void StartWinRocRecording(WinRocRegion region)
            {
                string folder = @"E:\ffmpegMovie";
                Directory.CreateDirectory(folder);

                string path = Path.Combine(folder, $"winroc_{DateTime.Now:HHmmss}.mp4");

                IsWinRocRecording = true;
                IsRecordingUiMode = true;

                await _winRoc.StartRecordingAsync(path, region);
            }
            private async Task StopWinRocAsync()
            {
                await _winRoc.StopAsync();

                if (Application.Current.MainWindow is Views.MovieEditorView view)
                {
                    view.HideRec();

                    // 🔥 録画終了後は枠を戻す
                    if (SelectedRegion != null)
                    {
                        view.ShowRecordingBorder(SelectedRegion.Value);
                    }
                }

                IsWinRocRecording = false;
                IsRecordingUiMode = false;
            }
        }
    }