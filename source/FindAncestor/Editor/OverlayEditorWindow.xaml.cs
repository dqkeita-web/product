using FindAncestor.Editor.FindAncestor.Editor;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FindAncestor.Editor
{
    public partial class OverlayEditorWindow : Window
    {
        private OverlayEditorViewModel _vm;

        public OverlayEditorWindow(OverlayEditorViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
            _vm = vm;
        }

        private void AddGroup_Click(object sender, RoutedEventArgs e)
        {
            _vm.AddGroup();
        }

        private void AddItem_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is OverlayGroup g)
                _vm.AddItem(g);
        }

        private void RemoveItem_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is OverlayItem item)
            {
                foreach (var g in _vm.Groups)
                {
                    if (g.Items.Contains(item))
                    {
                        _vm.RemoveItem(g, item);
                        break;
                    }
                }
            }
        }

        private void Color_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Background is Brush color)
            {
                foreach (var g in _vm.Groups)
                {
                    foreach (var item in g.Items)
                    {
                        item.Foreground = color;
                    }
                }
            }
        }
    }
}