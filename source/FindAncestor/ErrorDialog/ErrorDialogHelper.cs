using System.Windows;

namespace FindAncestor.ErrorDialog
{
    public static class ErrorDialogHelper
    {
        public static void Show(string message)
        {
            var app = Application.Current;

            if (app == null)
                return;

            if (app.Dispatcher.CheckAccess())
            {
                new ErrorDialogWindow(message).ShowDialog();
            }
            else
            {
                app.Dispatcher.Invoke(() =>
                {
                    new ErrorDialogWindow(message).ShowDialog();
                });
            }
        }
    }
}