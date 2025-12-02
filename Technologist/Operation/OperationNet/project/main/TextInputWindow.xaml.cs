using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ExtensionOperationsNet;

/// <summary>
/// Simple window to ask user for text input
/// </summary>
public partial class TextInputWindow
{
    /// <summary>
    /// User`s selected item from the text box
    /// </summary>
    public OperationTypeInfo? SelectedItem { get; private set; }

    /// <summary>
    /// Simple window to ask user for text input
    /// </summary>
    public TextInputWindow(List<OperationTypeInfo> items)
    {
        InitializeComponent();
        ItemsListBox.ItemsSource = items;
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        if (ItemsListBox.SelectedItem is OperationTypeInfo selected)
        {
            SelectedItem = selected;
            DialogResult = true;
            Close();
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        SelectedItem = null;
        DialogResult = false;
        Close();
    }

    private void ItemsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ItemsListBox.SelectedItem is OperationTypeInfo selected)
        {
            SelectedItem = selected;
            DialogResult = true;
            Close();
        }
    }

    private void ItemsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        OkButton.IsEnabled = ItemsListBox.SelectedItem != null;
    }
}