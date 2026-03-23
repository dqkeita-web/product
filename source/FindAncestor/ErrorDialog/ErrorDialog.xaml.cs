using System.Windows;

namespace FindAncestor.ErrorDialog
{
    public partial class ErrorDialog : Window
    {
        public string Message { get; }

        public ErrorDialog(string message)
        {
            InitializeComponent();
            Message = message;
            DataContext = this;
        }

        private void OnClose(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnCopy(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(Message);
            }
            catch { }
        }

        public static void Show(string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var dlg = new ErrorDialog(message)
                {
                    Owner = Application.Current.MainWindow
                };
                dlg.ShowDialog();
            });
        }
    }
}