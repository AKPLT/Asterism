using System.Windows;
using Asterism.Client.ViewModels;

namespace Asterism.Client.Views;

public partial class ToolDetailWindow : Window
{
    public ToolDetailWindow(ToolCardViewModel tool)
    {
        InitializeComponent();
        DataContext = tool;
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
}
