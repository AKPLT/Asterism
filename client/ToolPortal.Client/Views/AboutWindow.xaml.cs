using System.Reflection;
using System.Windows;

namespace ToolPortal.Client.Views;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();

        var version = Assembly.GetExecutingAssembly().GetName().Version;
        VersionText.Text = version != null
            ? $"バージョン {version.Major}.{version.Minor}.{version.Build}"
            : "バージョン 1.0.0";
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
}
