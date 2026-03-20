using System.Windows;
using System.Windows.Controls;

namespace FindAncestor.Views
{
    public partial class ScrollingPreviewView : UserControl
    {
        public ScrollingPreviewView()
        {
            InitializeComponent();
        }

        public double ImageHeight
        {
            get => (double)GetValue(ImageHeightProperty);
            set => SetValue(ImageHeightProperty, value);
        }

        public static readonly DependencyProperty ImageHeightProperty =
            DependencyProperty.Register(nameof(ImageHeight), typeof(double), typeof(ScrollingPreviewView), new PropertyMetadata(450.0));

        public double AspectRatio
        {
            get => (double)GetValue(AspectRatioProperty);
            set => SetValue(AspectRatioProperty, value);
        }

        public static readonly DependencyProperty AspectRatioProperty =
            DependencyProperty.Register(nameof(AspectRatio), typeof(double), typeof(ScrollingPreviewView), new PropertyMetadata(16.0 / 9.0));
    }
}