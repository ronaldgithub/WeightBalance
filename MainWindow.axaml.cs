using Avalonia.Controls;
using WeightBalance.ViewModels;

namespace WeightBalance;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}
