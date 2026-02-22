using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FindAncestor.Enum;
using FindAncestor.Models;
using FindAncestor.Views;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace FindAncestor.ViewModels
{
    public partial class MainMenuViewModel : ObservableObject
    {
        public ObservableCollection<DisplaySize> DisplaySizes { get; } = new();

        public ObservableCollection<AspectRatioItem> AspectRatios { get; } = new()
        {
            new AspectRatioItem("16:9", 16.0/9.0),
            new AspectRatioItem("4:3", 4.0/3.0),
            new AspectRatioItem("1:1", 1.0),
            new AspectRatioItem("3:2", 3.0/2.0)
        };

        [ObservableProperty]
        private double _scrollSpeed = 3; // 1～5

        [ObservableProperty]
        private AspectRatioItem _selectedAspectRatio = null!;

        [ObservableProperty]
        private double _imageWidth = 900;

        [ObservableProperty]
        private DisplaySize? _selectedDisplaySize;

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
        private void OpenScroll1Row()
        {
            var window = new Scroll1RowWindow();
            _scrollViewModel = new Scroll1RowViewModel(
                imageHeight: ImageWidth / SelectedAspectRatio.Value,
                aspectRatio: SelectedAspectRatio.Value,
                scrollSpeed: ScrollSpeed // 初期値
            );
            window.DataContext = _scrollViewModel;
            window.Show();

            // ScrollSpeed プロパティが変わるたびに ViewModel に反映
            this.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ScrollSpeed))
                {
                    _scrollViewModel.ScrollSpeed = this.ScrollSpeed;
                }
            };
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
        /// <summary>
        /// 横幅・縦横比・スクロール速度変更時に呼び出す
        /// </summary>
        partial void OnImageWidthChanged(double value)
        {
            _scrollViewModel?.UpdateSize(value, SelectedAspectRatio.Value);
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
        private async void ExportScrollVideo()
        {
            if (_scrollViewModel == null) return;

            int fps = 30;
            int durationSeconds = 10;
            int totalFrames = fps * durationSeconds;

            // 保存フォルダ
            string frameFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "frames");
            Directory.CreateDirectory(frameFolder);

            // FFmpeg の絶対パス
            string ffmpegPath = @"C:\Tools\ffmpeg\bin\ffmpeg.exe"; // ←環境に合わせて変更
            string outputPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scroll_video.mp4");

            // 現在のスクロール位置を保持
            double startScroll = _scrollViewModel.ScrollPosition;

            int frameIndex = 0;

            // CompositionTarget.Rendering を使って滑らかにキャプチャ
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

                // 1フレーム分スクロール
                _scrollViewModel.ScrollPosition += _scrollViewModel.ScrollSpeed / fps;

                // 無限スクロール用にリセット
                double totalWidth = 0;
                foreach (var img in _scrollViewModel.ScrollImages)
                    totalWidth += img.Width;
                if (_scrollViewModel.ScrollPosition > totalWidth / 2)
                    _scrollViewModel.ScrollPosition = 0;

                // ウィンドウを描画
                var scrollWindow = Application.Current.Windows.OfType<Scroll1RowWindow>()
                    .FirstOrDefault(w => w.DataContext == _scrollViewModel);
                if (scrollWindow == null) return;

                int width = (int)scrollWindow.ActualWidth;
                int height = (int)scrollWindow.ActualHeight;
                var rtb = new RenderTargetBitmap(width, height, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                rtb.Render(scrollWindow);

                // PNG 保存
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(rtb));
                string filePath = Path.Combine(frameFolder, $"frame_{frameIndex:D4}.png");
                using (var fs = new FileStream(filePath, FileMode.Create))
                    encoder.Save(fs);

                frameIndex++;
            };

            CompositionTarget.Rendering += handler;
            await tcs.Task;

            // ffmpeg で MP4 に変換
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = $"-y -framerate {fps} -i \"{frameFolder}\\frame_%04d.png\" -c:v libx264 -pix_fmt yuv420p \"{outputPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            System.Diagnostics.Process.Start(psi)?.WaitForExit();

            // スクロール位置を元に戻す
            _scrollViewModel.ScrollPosition = startScroll;

            MessageBox.Show($"動画の出力が完了しました。\nファイル: {outputPath}");
        }
    }
}