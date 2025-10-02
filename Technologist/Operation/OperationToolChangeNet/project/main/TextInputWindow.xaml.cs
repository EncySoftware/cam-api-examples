using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ExtensionOperationToolNet;

/// <summary>
/// Simple window to ask user for text input
/// </summary>
public partial class TextInputWindow
{
    /// <summary>
    /// User input from the text box
    /// </summary>
    public string? UserInput { get; private set; }

    /// <summary>
    /// Simple window to ask user for text input
    /// </summary>
    public TextInputWindow(List<string> items)
    {
        InitializeComponent();
        ItemsListBox.ItemsSource = items;
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}