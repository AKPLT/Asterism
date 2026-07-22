using System.Windows;
using ToolPortal.Client.Converters;
using ToolPortal.Client.Services;

namespace ToolPortal.Client.Views;

public partial class ServerSettingsDialog : Window
{
    private readonly IUserSettingsService _userSettingsService;

    public ServerSettingsDialog(IUserSettingsService userSettingsService)
    {
        InitializeComponent();
        _userSettingsService = userSettingsService;
        UrlInput.Text = _userSettingsService.ServerBaseUrl;
    }

    private void OnOkClick(object sender, RoutedEventArgs e)
    {
        ErrorText.Visibility = Visibility.Collapsed;

        var input = UrlInput.Text.Trim();
        if (!input.EndsWith('/'))
        {
            input += "/";
        }

        if (!Uri.TryCreate(input, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            ShowError("http:// または https:// で始まる正しいURLを入力してください。");
            return;
        }

        _userSettingsService.ServerBaseUrl = uri.ToString();
        _userSettingsService.Save();
        StringToImageSourceConverter.BaseUri = uri;

        DialogResult = true;
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }
}
