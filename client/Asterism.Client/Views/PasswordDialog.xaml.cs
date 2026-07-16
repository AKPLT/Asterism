using System.Windows;
using Asterism.Client.Services;
using Asterism.Client.Services.Exceptions;

namespace Asterism.Client.Views;

public partial class PasswordDialog : Window
{
    private readonly IAdminApiService _adminApiService;

    public PasswordDialog(IAdminApiService adminApiService)
    {
        InitializeComponent();
        _adminApiService = adminApiService;
    }

    private async void OnOkClick(object sender, RoutedEventArgs e)
    {
        ErrorText.Visibility = Visibility.Collapsed;
        OkButton.IsEnabled = false;

        try
        {
            var success = await _adminApiService.TryUnlockAsync(PasswordInput.Password);
            if (success)
            {
                DialogResult = true;
            }
            else
            {
                ShowError("パスワードが違います。");
            }
        }
        catch (AdminApiException ex)
        {
            ShowError(ex.Message);
        }
        finally
        {
            OkButton.IsEnabled = true;
        }
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
