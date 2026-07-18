using System.Windows;
using System.Windows.Controls;
using Asterism.Client.ViewModels;

namespace Asterism.Client.Views;

public partial class ToolCardView : UserControl
{
    public ToolCardView()
    {
        InitializeComponent();
    }

    private void OnCardClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (DataContext is not ToolCardViewModel tool) return;

        var detailWindow = new ToolDetailWindow(tool)
        {
            Owner = Window.GetWindow(this)
        };
        detailWindow.Show();
    }
}
