namespace FindAncestor.Editor
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using FindAncestor.ErrorDialog;

    public partial class ColorPickerWindow : Window
    {
        private ColorPickerViewModel vm;

        public ColorPickerWindow()
        {
            try
            {
                InitializeComponent();

                vm = new ColorPickerViewModel
                {
                    R = 0,
                    G = 0,
                    B = 0
                };

                DataContext = vm;
            }
            catch (Exception ex)
            {
                ErrorDialogService.Show(ex);
            }
        }

        private void ColorArea_Mouse(object sender, MouseEventArgs e)
        {
            try
            {
                var pos = e.GetPosition(ColorArea);

                Canvas.SetLeft(Cursor, pos.X - 5);
                Canvas.SetTop(Cursor, pos.Y - 5);

                double x = pos.X / 260;
                double y = pos.Y / 260;

                vm.R = (int)(255 * x);
                vm.G = (int)(255 * (1 - y));
                vm.B = (int)(255 * (1 - x));
            }
            catch (Exception ex)
            {
                ErrorDialogService.Show(ex);
            }
        }

        private void HueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                double h = e.NewValue;

                vm.R = (int)(Math.Abs(Math.Sin(h)) * 255);
                vm.G = (int)(Math.Abs(Math.Sin(h + 2)) * 255);
                vm.B = (int)(Math.Abs(Math.Sin(h + 4)) * 255);
            }
            catch (Exception ex)
            {
                ErrorDialogService.Show(ex);
            }
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ErrorDialogService.Show(ex);
            }
        }
    }
}