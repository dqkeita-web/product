using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Windows;

namespace FindAncestor
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            try
            {
                // 念のため全プロセスkill
                foreach (var p in Process.GetProcessesByName("ffmpeg"))
                {
                    p.Kill();
                }
            }
            catch { }
        }
    }

}
