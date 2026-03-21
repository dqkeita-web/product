using FindAncestor.ViewModels;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace FindAncestor.Views
{
    public partial class MovieEditorView : Window
    {
        private readonly DispatcherTimer _uiTimer = new() { Interval = TimeSpan.FromSeconds(2) };

        public Size GetPreviewSize()
        {
            return new Size(RootGrid.ActualWidth, RootGrid.ActualHeight);
        }

        public MovieEditorView()
        {
            InitializeComponent();

            var vm = new MovieEditorViewModel();
            DataContext = vm;

            // 🔥 ここでイベント購読
            vm.RecordingCompleted += OnRecordingCompleted;
        }
        public void PlayLatestVideo()
        {
            string folder = @"E:\ffmpegMovie";

            if (!Directory.Exists(folder)) return;

            var file = Directory.GetFiles(folder, "*.mp4")
                .OrderByDescending(File.GetLastWriteTime)
                .FirstOrDefault();

            if (file == null) return;

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = file,
                    UseShellExecute = true
                });
            }
            catch { }
        }
        public void PlayLatestVideo(string path)
        {
            if (!File.Exists(path)) return;

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
            }
            catch { }
        }
        // 🔥 録画完了 → 再生
        private void OnRecordingCompleted(string path)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = path,
                        UseShellExecute = true
                    });
                }
                catch
                {
                    Debug.WriteLine("動画再生失敗: " + path);
                }
            });
        }

        private bool _isRightDragging = false;
        private Point _mouseStartScreen;
        private Point _windowStart;

        private void OnSizeSliderReleased(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is MovieEditorViewModel vm)
            {
                vm.ApplyImageWidth();
            }
        }

        private void RootGrid_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isRightDragging = true;
            _mouseStartScreen = PointToScreen(e.GetPosition(this));
            _windowStart = new Point(this.Left, this.Top);
            RootGrid.CaptureMouse();
        }

        private void RootGrid_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isRightDragging = false;
            RootGrid.ReleaseMouseCapture();
        }

        private void RootGrid_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isRightDragging && e.RightButton == MouseButtonState.Pressed)
            {
                Point currentScreen = PointToScreen(e.GetPosition(this));
                Vector delta = currentScreen - _mouseStartScreen;

                this.Left = _windowStart.X + delta.X;
                this.Top = _windowStart.Y + delta.Y;
            }
        }

        private void HideUI(object? sender, EventArgs e)
        {
            if (DataContext is MovieEditorViewModel vm)
                vm.UiOpacity = 0;

            _uiTimer.Stop();
        }

        private void OnSliderReleased(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is MovieEditorViewModel vm)
                vm.ApplyImageWidth();
        }

        private void OnDrop(object sender, DragEventArgs e)
        {
            if (DataContext is not MovieEditorViewModel vm) return;
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

            var files = (string[])e.Data.GetData(DataFormats.FileDrop);

            foreach (var file in files)
            {
                if (!file.EndsWith(".png", StringComparison.OrdinalIgnoreCase) &&
                    !file.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) &&
                    !file.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
                    continue;

                vm.AddImage(file);
            }
        }
    }
}