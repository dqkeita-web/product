namespace FindAncestor.Editor
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using FindAncestor.ErrorDialog;

    public partial class OverlayEditorWindow : Window
    {
        private OverlayEditorViewModel _vm;

        public OverlayEditorWindow(OverlayEditorViewModel vm)
        {
            try
            {
                InitializeComponent();
                DataContext = vm;
                _vm = vm;
            }
            catch (Exception ex)
            {
                ErrorDialogService.Show(ex);
            }
        }

        private void AddItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if ((sender as Button)?.DataContext is OverlayGroup g)
                    _vm.AddItem(g);
            }
            catch (Exception ex)
            {
                ErrorDialogService.Show(ex);
            }
        }

        private void RemoveItem_Click(object sender, RoutedEventArgs e)
        {
            try
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
            catch (Exception ex)
            {
                ErrorDialogService.Show(ex);
            }
        }

        private void Item_Focus(object sender, RoutedEventArgs e)
        {
            try
            {
                if ((sender as FrameworkElement)?.DataContext is OverlayItem item)
                    _vm.SelectedItem = item;
            }
            catch (Exception ex)
            {
                ErrorDialogService.Show(ex);
            }
        }

        // 🎨 カラーピッカー
        private void ColorPicker_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if ((sender as Button)?.Tag is not OverlayItem item)
                    return;

                var picker = new ColorPickerWindow();

                var result = picker.ShowDialog(); // ← 落ちる箇所

                if (result == true)
                {
                    if (picker.DataContext is ColorPickerViewModel vm)
                    {
                        item.Foreground = vm.SelectedBrush;
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorDialogService.Show(ex);
            }
        }
    }
}