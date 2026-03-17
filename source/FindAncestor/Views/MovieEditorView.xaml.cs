using FindAncestor.ViewModels;
using System.Windows;

namespace FindAncestor.Views;

public partial class MovieEditorView : Window
{
    public MovieEditorView()
    {
        InitializeComponent();
        DataContext = new MovieEditorViewModel();
    }
}