using System.Windows;

namespace FindAncestor.ErrorDialog
{
    public partial class ErrorDialogWindow : Window
    {
        public string Message { get; }

        public ErrorDialogWindow(string message)
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
            catch
            {
            }
        }
    }
}