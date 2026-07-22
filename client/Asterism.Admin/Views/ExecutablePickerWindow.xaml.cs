using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace Asterism.Admin.Views;

public partial class ExecutablePickerWindow : Window
{
    public string? SelectedPath { get; private set; }

    public ExecutablePickerWindow(IReadOnlyList<string> candidates)
    {
        InitializeComponent();

        CandidatesListBox.ItemsSource = candidates;
        if (candidates.Count > 0)
            CandidatesListBox.SelectedIndex = 0;
    }

    private void OnSelectClick(object sender, RoutedEventArgs e) => Confirm();

    private void OnListBoxDoubleClick(object sender, MouseButtonEventArgs e) => Confirm();

    private void Confirm()
    {
        if (CandidatesListBox.SelectedItem is string path)
        {
            SelectedPath = path;
            DialogResult = true;
        }
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
