using System.Windows;

namespace Asterism.Client.Views;

public partial class AdminHelpWindow : Window
{
    public AdminHelpWindow()
    {
        InitializeComponent();
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
}
