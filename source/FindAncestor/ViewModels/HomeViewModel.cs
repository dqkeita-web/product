using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FindAncestor.Enum;
using System;
using System.Windows;

namespace FindAncestor.ViewModels
{
    public partial class HomeViewModel : ObservableObject
    {
        public ImageViewModel ParentA { get; }
        public ImageViewModel ParentB { get; }
        public ImageViewModel ParentC { get; }
        public ImageViewModel ParentD { get; }

        public HomeViewModel(DisplayMode mode)
        {
            ParentA = new ImageViewModel("A", mode);
            ParentB = new ImageViewModel("B", mode);
            ParentC = new ImageViewModel("C", mode);
            ParentD = new ImageViewModel("D", mode);
        }

        [RelayCommand]
        private void Back()
        {
            DisposeAll();

            var menu = new MainMenuView();
            menu.Show();

            Application.Current.Windows[0]?.Close();
        }

        private void DisposeAll()
        {
            ParentA.Dispose();
            ParentB.Dispose();
            ParentC.Dispose();
            ParentD.Dispose();
        }
    }
}