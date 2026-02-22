using System.Windows;
using System.Windows.Controls;

namespace FindAncestor.Views
{
    public partial class Scroll1RowView : UserControl
    {
        public Scroll1RowView()
        {
            InitializeComponent();
        }

        // 画像の高さを指定
        public double ImageHeight
        {
            get { return (double)GetValue(ImageHeightProperty); }
            set { SetValue(ImageHeightProperty, value); }
        }

        public static readonly DependencyProperty ImageHeightProperty =
            DependencyProperty.Register(nameof(ImageHeight), typeof(double), typeof(Scroll1RowView), new PropertyMetadata(450.0));

        // 縦横比（Width / Height）
        public double AspectRatio
        {
            get { return (double)GetValue(AspectRatioProperty); }
            set { SetValue(AspectRatioProperty, value); }
        }

        public static readonly DependencyProperty AspectRatioProperty =
            DependencyProperty.Register(nameof(AspectRatio), typeof(double), typeof(Scroll1RowView), new PropertyMetadata(16.0 / 9.0));
    }
}