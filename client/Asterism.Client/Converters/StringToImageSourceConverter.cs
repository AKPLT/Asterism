using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace Asterism.Client.Converters;

public sealed class StringToImageSourceConverter : IValueConverter
{
    /// <summary>相対URLの解決に使うサーバーのBaseAddress。App起動時に設定する。</summary>
    public static Uri? BaseUri { get; set; }

    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string url || string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        try
        {
            var uri = BaseUri != null ? new Uri(BaseUri, url) : new Uri(url, UriKind.RelativeOrAbsolute);
            var image = new BitmapImage();
            image.BeginInit();
            image.UriSource = uri;
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.EndInit();
            return image;
        }
        catch (Exception ex) when (ex is UriFormatException or NotSupportedException)
        {
            return null;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
