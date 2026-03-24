namespace FindAncestor.Editor
{
    using System;
    using System.Windows.Media;
    using FindAncestor.ErrorDialog;

    public class ColorPickerViewModel : BindableBase
    {
        private int _r;
        public int R
        {
            get => _r;
            set
            {
                try
                {
                    value = Math.Clamp(value, 0, 255);
                    if (Set(ref _r, value))
                        Update();
                }
                catch (Exception ex)
                {
                    ErrorDialogService.Show(ex);
                }
            }
        }

        private int _g;
        public int G
        {
            get => _g;
            set
            {
                try
                {
                    value = Math.Clamp(value, 0, 255);
                    if (Set(ref _g, value))
                        Update();
                }
                catch (Exception ex)
                {
                    ErrorDialogService.Show(ex);
                }
            }
        }

        private int _b;
        public int B
        {
            get => _b;
            set
            {
                try
                {
                    value = Math.Clamp(value, 0, 255);
                    if (Set(ref _b, value))
                        Update();
                }
                catch (Exception ex)
                {
                    ErrorDialogService.Show(ex);
                }
            }
        }

        public string Hex => $"#{R:X2}{G:X2}{B:X2}";

        private Brush _selectedBrush = Brushes.Black;
        public Brush SelectedBrush
        {
            get => _selectedBrush;
            set => Set(ref _selectedBrush, value);
        }

        private void Update()
        {
            try
            {
                var c = Color.FromRgb((byte)R, (byte)G, (byte)B);
                SelectedBrush = new SolidColorBrush(c);

                OnPropertyChanged(nameof(Hex));
            }
            catch (Exception ex)
            {
                ErrorDialogService.Show(ex);
            }
        }
    }
}