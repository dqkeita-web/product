using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FindAncestor.Editor;
using FindAncestor.Enum;
using FindAncestor.ErrorDialog;
using FindAncestor.Models;
using FindAncestor.Roc;
using FindAncestor.Services;
using FindAncestor.Views;
using FindAncestor.Voice;
using FindAncestor.Voice.Character;
using FindAncestor.Voice.LLM;
using FindAncestor.Voice.OCR;
using FindAncestor.WinRoc;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using WinRocRecorder = FindAncestor.WinRoc.WinRocRecorder;

namespace FindAncestor.ViewModels
{
    // ========== UI状態・プレビュー ==========
    public partial class MovieEditorViewModel : ObservableObject
    {
        // ===== フィールド =====

        private readonly RecordingEngine _engine = new();
        private readonly WinRocRecorder _winRoc;
        private readonly DispatcherTimer _autoPlayTimer;
        private readonly Stopwatch _playClock = new();
        private readonly Stopwatch _timeline = new();
        private readonly DefaultTextService _textService = new();
        private readonly VideoTrimService _trimService = new();
        private OcrPipelineService? _ocrPipeline;
        private VoicePipelineService? _voicePipeline;

        private ScrollingPreviewViewModel? _scrollingPreviewViewModel;
        private OverlayEditorViewModel? _editorVM;
        private FrameworkElement? _captureTarget;
        private WinRocRegion? _lastSelectedRegion;
        private RenderTargetBitmap? _rtb;
        private string? _currentTrimFile;

        private double _currentScrollPos;
        private double _recordStartTime;
        private double _currentTime;
        private int _engineWidth;
        private int _engineHeight;
        private bool _isRecordingInternal;

        // ===== プロパティ（UI状態） =====

        [ObservableProperty] private bool _isRecording;
        [ObservableProperty] private bool _isRecordingUiMode;
        [ObservableProperty] private bool _isPlaying;
        [ObservableProperty] private bool _isEmbeddedMode;
        [ObservableProperty] private bool _isPresetMode;
        [ObservableProperty] private bool _isWinRocRecording;
        [ObservableProperty] private bool _isRegionSelecting;
        [ObservableProperty] private bool _isSelectingRegion;
        [ObservableProperty] private bool _isEditingRegion;

        [ObservableProperty] private double _uiOpacity = 1;
        [ObservableProperty] private double _imageWidth = 900;
        [ObservableProperty] private double _scrollSpeed = 3;
        [ObservableProperty] private double _volume = 0.5;
        [ObservableProperty] private double _fadeDuration = 1.5;

        [ObservableProperty] private Rect? _selectedRegion;
        [ObservableProperty] private AspectRatioItem _selectedAspectRatio = new("16:9", 16.0 / 9.0);
        [ObservableProperty] private ImageFolderType _selectedPlayFolder = ImageFolderType.A;
        [ObservableProperty] private DisplaySize? _selectedDisplaySize;
        [ObservableProperty] private FolderPanelMode _currentPanelMode = FolderPanelMode.None;
        [ObservableProperty] private SliderPanelMode _currentSliderMode = SliderPanelMode.None;

        [ObservableProperty] private ObservableCollection<string> _audioFiles = new();
        [ObservableProperty] private int _currentAudioIndex;
        [ObservableProperty] private bool _isLoopEnabled = true;
        [ObservableProperty] private ObservableCollection<FolderPreviewItem> _folderPreviews = new();
        [ObservableProperty] private ScrollingPreviewViewModel? _embeddedPreviewViewModel;

        // ===== プロパティ（トリム） =====

        [ObservableProperty] private double trimStartSeconds;
        [ObservableProperty] private double trimEndSeconds;
        [ObservableProperty] private double videoDurationSeconds;
        [ObservableProperty] private double videoDuration;
        [ObservableProperty] private double trimStartPosition;
        [ObservableProperty] private double trimEndPosition;
        [ObservableProperty] private ObservableCollection<BitmapImage> frameImages = new();
        [ObservableProperty] private string? selectedVideoPath;
        [ObservableProperty] private bool isTrimmingMode;
        [ObservableProperty] private TimeSpan trimStart;
        [ObservableProperty] private TimeSpan trimEnd;

        public double TrimWidth => TrimEndPosition - TrimStartPosition;
        public double CurrentTime => _timeline.Elapsed.TotalSeconds;
        public OverlayEditorViewModel? EditorVM => _editorVM;

        public RelayCommand SpeakCommand { get; }

        // ===== イベント =====

        public event Action<string>? RecordingCompleted;
        public event Action<string>? RequestLoadTrimVideo;

        // ===== コンストラクタ =====

        public MovieEditorViewModel()
        {
            _engine.RecordingCompleted += path => RecordingCompleted?.Invoke(path);
            _winRoc = new WinRocRecorder(_engine);

            _autoPlayTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            _autoPlayTimer.Tick += AutoPlayTick;

            LoadFolderPreviews();
            SpeakCommand = InitializeVoice();
        }

        private RelayCommand InitializeVoice()
        {
            try
            {
                var config = VoiceConfigLoader.Load("voice_config.json");
                var tts = new TtsClient(
                    "http://localhost:50021",
                    @"C:\Program Files\VOICEVOX\VOICEVOX.exe");
                var playback = new VoicePlaybackService();
                var emotion = new PythonEmotionClient();
                var character = new CharacterVoiceService(config);
                var ocrService = new OcrService();
                var ocrCache = new OcrCacheService();
                var variation = new VoiceVariationService();
                var llm = new PythonLlmClient();
                var dialogue = new DialogueAiClient();
                var validator = new TextValidationService();

                _ocrPipeline = new OcrPipelineService(ocrService, ocrCache);
                _voicePipeline = new VoicePipelineService(tts, playback, emotion, variation, validator);

                // ローカル変数に受けてからラムダでキャプチャする（out/ref はラムダ内でキャプチャ不可のため）
                var pipeline = _voicePipeline;

                return new RelayCommand(async () =>
                {
                    try
                    {
                        var lines = VoiceScriptService.GetDemoLines();
                        await pipeline.SpeakLinesAsync(lines);
                    }
                    catch (Exception ex)
                    {
                        ErrorDialogService.Show(ex);
                    }
                });
            }
            catch (Exception ex)
            {
                ErrorDialogService.Show("Voice初期化失敗: " + ex);
                return new RelayCommand(() => { });
            }
        }

        // ===== Command（フォルダ・画像） =====

        [RelayCommand]
        private void OpenEmbeddedScroll()
        {
            double height = ImageWidth / SelectedAspectRatio.Value;
            EmbeddedPreviewViewModel = new ScrollingPreviewViewModel(height, SelectedAspectRatio.Value, ScrollSpeed);
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

        // ===== Command（パネルトグル） =====

        [RelayCommand]
        private void ToggleAddPanel() =>
            CurrentPanelMode = CurrentPanelMode == FolderPanelMode.Add ? FolderPanelMode.None : FolderPanelMode.Add;

        [RelayCommand]
        private void ToggleDeletePanel() =>
            CurrentPanelMode = CurrentPanelMode == FolderPanelMode.Delete ? FolderPanelMode.None : FolderPanelMode.Delete;

        [RelayCommand]
        private void TogglePlayPanel() =>
            CurrentPanelMode = CurrentPanelMode == FolderPanelMode.Play ? FolderPanelMode.None : FolderPanelMode.Play;

        [RelayCommand]
        private void ToggleVolumeSlider() =>
            CurrentSliderMode = CurrentSliderMode == SliderPanelMode.Volume ? SliderPanelMode.None : SliderPanelMode.Volume;

        [RelayCommand]
        private void ToggleSpeedSlider() =>
            CurrentSliderMode = CurrentSliderMode == SliderPanelMode.Speed ? SliderPanelMode.None : SliderPanelMode.Speed;

        [RelayCommand]
        private void ToggleSizeSlider() =>
            CurrentSliderMode = CurrentSliderMode == SliderPanelMode.Size ? SliderPanelMode.None : SliderPanelMode.Size;

        // ===== Command（再生・音声） =====

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

        [RelayCommand]
        private void SelectAudio()
        {
            var dialog = new OpenFileDialog { Filter = "Audio Files|*.mp3;*.wav", Multiselect = true };
            if (dialog.ShowDialog() == true)
            {
                AudioFiles.Clear();
                foreach (var file in dialog.FileNames)
                    AudioFiles.Add(file);
                CurrentAudioIndex = 0;
            }
        }

        // ===== Command（録画） =====

        [RelayCommand]
        private async Task ToggleRecording()
        {
            try
            {
                if (IsRecording)
                    await StopRecordingAsync();
                else
                    StartRecordingInternal();
            }
            catch (Exception ex)
            {
                ErrorDialogService.Show(ex);
            }
        }

        [RelayCommand]
        private async Task ToggleWinRocRecording()
        {
            try
            {
                if (IsWinRocRecording)
                    await StopWinRocAsync();
                else
                    await StartWinRocRecording();
            }
            catch (Exception ex)
            {
                ErrorDialogService.Show(ex);
            }
        }

        [RelayCommand]
        public void StartRegionSelect()
        {
            if (Application.Current.MainWindow is not Views.MovieEditorView v) return;
            v.StartRegionSelect();
        }

        [RelayCommand]
        public void ToggleRegionEdit()
        {
            IsEditingRegion = !IsEditingRegion;
        }

        // ===== Command（トリム） =====

        [RelayCommand]
        private void StartTrimFlow()
        {
            if (!IsTrimmingMode)
            {
                var dlg = new Microsoft.Win32.OpenFileDialog { Filter = "Video (*.mp4)|*.mp4" };
                if (dlg.ShowDialog() != true) return;
                _currentTrimFile = dlg.FileName;
                RequestLoadTrimVideo?.Invoke(_currentTrimFile);
                IsTrimmingMode = true;
            }
            else
            {
                ExecuteTrim();
                IsTrimmingMode = false;
            }
        }

        [RelayCommand]
        private async Task LoadFrames(string path)
        {
            FrameImages.Clear();
            string tempDir = Path.Combine(Path.GetTempPath(), "trim_frames");
            Directory.CreateDirectory(tempDir);
            string ffmpeg = @"C:\Tools\ffmpeg\bin\ffmpeg.exe";
            string args = $"-i \"{path}\" -vf fps=1 \"{tempDir}\\frame_%03d.jpg\"";

            var p = Process.Start(new ProcessStartInfo
            {
                FileName = ffmpeg,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            await p.WaitForExitAsync();

            foreach (var file in Directory.GetFiles(tempDir))
                FrameImages.Add(new BitmapImage(new Uri(file)));
        }

        // ===== Command（エディタ・その他） =====

        [RelayCommand]
        private void OpenEditor()
        {
            try
            {
                if (_editorVM == null)
                    _editorVM = new OverlayEditorViewModel();
                var win = new OverlayEditorWindow(_editorVM);
                win.Show();
            }
            catch (Exception ex)
            {
                ErrorDialogService.Show(ex);
            }
        }

        [RelayCommand]
        private void OpenAiVoice()
        {
            try
            {
                var win = new FindAncestor.AiVoice.AiVoiceView();
                win.Show();
            }
            catch (Exception ex)
            {
                ErrorDialogService.Show(ex);
            }
        }

        [RelayCommand]
        private void CloseApplication()
        {
            try { _engine.Stop(); } catch { }
            Application.Current.Shutdown();
        }

        // ===== メソッド（画像・プレビュー） =====

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
            target.ScrollImages.Add(new ImageWithWidth { Source = bitmap, Width = width, Height = height });
        }

        public void ApplyImageWidth()
        {
            UpdatePreviewSize(ImageWidth, SelectedAspectRatio.Value);
            LoadImagesFromFolder(SelectedPlayFolder);
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
                AddImage(file);
        }

        private void UpdatePreviewSize(double width, double aspect)
        {
            _scrollingPreviewViewModel?.UpdateSize(width, aspect);
            EmbeddedPreviewViewModel?.UpdateSize(width, aspect);
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
                var firstImage = Directory.Exists(path) ? Directory.GetFiles(path).FirstOrDefault() : null;
                BitmapImage? thumb = firstImage != null ? LoadImage(firstImage) : null;
                FolderPreviews.Add(new FolderPreviewItem { Folder = folder, Thumbnail = thumb, Color = color });
            }
        }

        // ===== メソッド（音声） =====

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

        // ===== メソッド（自動再生） =====

        private void StartAutoPlay()
        {
            var vm = _scrollingPreviewViewModel ?? EmbeddedPreviewViewModel;
            if (vm == null) return;
            _currentScrollPos = vm.ScrollPosition;
            _playClock.Restart();
            if (!_autoPlayTimer.IsEnabled) _autoPlayTimer.Start();
        }

        private void AutoPlayTick(object? sender, EventArgs e)
        {
            try
            {
                var vm = _scrollingPreviewViewModel ?? EmbeddedPreviewViewModel;
                if (vm == null) return;

                double now = _playClock.Elapsed.TotalSeconds;
                double delta = now - _recordStartTime;
                _recordStartTime = now;
                _currentScrollPos += ScrollSpeed * delta;
                vm.ScrollPosition = _currentScrollPos;
            }
            catch (Exception ex)
            {
                ErrorDialogService.Show(ex);
            }
        }

        // ===== メソッド（録画） =====

        private void StartRecordingInternal()
        {
            try
            {
                var view = Application.Current.MainWindow as Views.MovieEditorView;
                if (view == null) { ErrorDialogService.Show("View取得失敗"); return; }

                var captureTarget = view.VideoHost;
                int width = (int)captureTarget.ActualWidth;
                int height = (int)captureTarget.ActualHeight;

                if (width <= 0 || height <= 0)
                {
                    ErrorDialogService.Show($"録画サイズが0: {width}x{height}");
                    return;
                }

                width = width / 2 * 2;
                height = height / 2 * 2;
                StartRecording(captureTarget, CreatePath(), width, height);
            }
            catch (Exception ex)
            {
                ErrorDialogService.Show(ex);
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
            try
            {
                if (!_isRecordingInternal || _captureTarget == null) return;

                int width = _engineWidth;
                int height = _engineHeight;

                if (width <= 0 || height <= 0)
                {
                    ErrorDialogService.Show($"録画サイズ不正: {width}x{height}");
                    return;
                }

                width = width / 2 * 2;
                height = height / 2 * 2;

                if (_rtb == null || _rtb.PixelWidth != width || _rtb.PixelHeight != height)
                    _rtb = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);

                var dv = new DrawingVisual();
                using (var dc = dv.RenderOpen())
                {
                    var vb = new VisualBrush(_captureTarget) { Stretch = Stretch.Fill };
                    dc.DrawRectangle(vb, null, new Rect(0, 0, width, height));
                    if (EditorVM != null)
                        OverlayRenderer.Draw(dc, EditorVM.Groups, CurrentTime);
                }

                _rtb.Clear();
                _rtb.Render(dv);

                var wb = new WriteableBitmap(_rtb);
                wb.Freeze();
                _engine.EnqueueFrame(wb);
            }
            catch (Exception ex)
            {
                ErrorDialogService.Show(ex);
            }
        }

        // ===== メソッド（WinRoc録画） =====

        private async Task StartWinRocRecording()
        {
            try
            {
                if (_lastSelectedRegion == null) { ErrorDialogService.Show("録画範囲未選択"); return; }

                var region = _lastSelectedRegion.Value;
                if (!region.IsValid) { ErrorDialogService.Show("録画領域が無効です"); return; }
                if (region.Width <= 0 || region.Height <= 0)
                {
                    ErrorDialogService.Show($"領域サイズ不正: {region.Width}x{region.Height}");
                    return;
                }

                IsWinRocRecording = true;
                IsRecordingUiMode = true;
                await _winRoc.StartRecordingAsync(CreatePath(), region);
            }
            catch (Exception ex)
            {
                ErrorDialogService.Show(ex);
            }
        }

        private async Task StopWinRocAsync()
        {
            await _winRoc.StopAsync();
            if (Application.Current.MainWindow is Views.MovieEditorView view)
            {
                view.HideRec();
                if (SelectedRegion != null)
                    view.ShowRecordingBorder(SelectedRegion.Value);
            }
            IsWinRocRecording = false;
            IsRecordingUiMode = false;
        }

        public void OnRegionSelected(Rect rect)
        {
            SelectedRegion = rect;
            if (Application.Current.MainWindow is Views.MovieEditorView view)
            {
                view.ShowRecordingBorder(rect);
                var screenTopLeft = view.PointToScreen(new Point(rect.X, rect.Y));
                var region = new WinRocRegion
                {
                    X = (int)screenTopLeft.X,
                    Y = (int)screenTopLeft.Y,
                    Width = Math.Max(1, (int)rect.Width),
                    Height = Math.Max(1, (int)rect.Height)
                };
                if (!region.IsValid) { ErrorDialogService.Show("選択領域が不正です"); return; }
                _lastSelectedRegion = region;
            }
        }

        // ===== メソッド（トリム） =====

        private async void ExecuteTrim()
        {
            if (_currentTrimFile == null) return;

            var ffmpegPath = @"C:\Tools\ffmpeg\bin\ffmpeg.exe";
            if (!File.Exists(ffmpegPath)) { ErrorDialogService.Show("❌ ffmpeg.exeが見つかりません"); return; }

            string outputDir = Path.Combine(Path.GetDirectoryName(_currentTrimFile)!, "trim");
            Directory.CreateDirectory(outputDir);
            string output = Path.Combine(outputDir, $"trim_{DateTime.Now:yyyyMMddHHmmss}.mp4");

            string args = $"-y -ss {TrimStartSeconds} -to {TrimEndSeconds} " +
                          $"-i \"{_currentTrimFile}\" -c copy \"{output}\"";

            var p = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            p.Start();
            await p.WaitForExitAsync();

            if (!File.Exists(output)) { ErrorDialogService.Show("❌ トリミング失敗"); return; }
            RecordingCompleted?.Invoke(output);
        }

        private string CreatePath()
        {
            string folder = @"E:\ffmpegMovie";
            Directory.CreateDirectory(folder);
            return Path.Combine(folder, $"winroc_{DateTime.Now:HHmmss}.mp4");
        }

        // ===== メソッド（プロパティ変更ハンドラ） =====

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

        partial void OnIsTrimmingModeChanged(bool value)
        {
            if (value)
            {
                EmbeddedPreviewViewModel = null;
                _scrollingPreviewViewModel = null;
                CompositionTarget.Rendering -= OnRenderFrame;
            }
        }
    }
}
