using System;
using System.Windows;
using System.Windows.Threading;

namespace FindAncestor.ErrorDialog
{
    public static class ErrorDialogService
    {
        public static void Show(Exception ex)
        {
            Show(ex.ToString());
        }

        public static void Show(string message)
        {
            try
            {
                if (Application.Current == null)
                    return;

                if (Application.Current.Dispatcher.CheckAccess())
                {
                    new ErrorDialogWindow(message).ShowDialog();
                }
                else
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        new ErrorDialogWindow(message).ShowDialog();
                    });
                }
            }
            catch
            {
                MessageBox.Show(message);
            }
        }
    }
}