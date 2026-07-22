using System.Windows;
using ToolPortal.Client.ViewModels;

namespace ToolPortal.Client.Views;

public partial class ToolDetailWindow : Window
{
    public ToolDetailWindow(ToolCardViewModel tool)
    {
        InitializeComponent();
        DataContext = tool;
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
}
