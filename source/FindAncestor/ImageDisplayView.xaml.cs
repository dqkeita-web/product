using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FindAncestor
{
    public partial class ImageDisplayView : UserControl
    {
        public ImageDisplayView()
        {
            InitializeComponent();
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            int startIndex = 0;

            if (Tag is string s && int.TryParse(s, out int parsed))
                startIndex = parsed;
            else if (Tag is int i)
                startIndex = i;

            DataContext = new ImageViewModel(startIndex);
        }
    }
}