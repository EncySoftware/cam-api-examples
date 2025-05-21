using System.Windows;

namespace ExtensionUtilityNotifyNet;

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
    public TextInputWindow()
    {
        InitializeComponent();
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        UserInput = InputBox.Text;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        UserInput = null;
        DialogResult = false;
        Close();
    }
}