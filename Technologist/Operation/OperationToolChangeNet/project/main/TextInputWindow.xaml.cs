using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;

namespace ExtensionOperationToolPopupNet;

/// <summary>
/// Simple window to ask user for text input
/// </summary>
public partial class TextInputWindow
{
    /// <summary>
    /// Path for file dialog
    /// </summary>
    public string InitialDirectory { get; set; } = @"C:\ProgramData\ENCY Software\ENCY NB\Version 1\Libraries\Tools\Examples";

    /// <summary>
    /// Selected file path
    /// </summary>
    public string? SelectedFilePath { get; private set; }
    public string? LibraryName { get; private set; }
    public string? ToolId { get; private set; }
    /// <summary>
    /// Simple window to ask user for text input
    /// </summary>
    public TextInputWindow(List<string> items)
    {
        InitializeComponent();
        ItemsListBox.ItemsSource = items;
    }

    private void Import_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = "Database files (*.db)|*.db|All files (*.*)|*.*",
            FilterIndex = 1,
            InitialDirectory = InitialDirectory,
            Title = "Select database file"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            SelectedFilePath = openFileDialog.FileName;
            NameTextBox.Text = System.IO.Path.GetFileNameWithoutExtension(SelectedFilePath);           
        }
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        LibraryName = NameTextBox.Text;
        ToolId = IdTextBox.Text; 
        
        if (string.IsNullOrWhiteSpace(LibraryName))
        {
            MessageBox.Show("Please enter library name", "Error", 
                          MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(ToolId))
        {
            MessageBox.Show("Please enter tool ID", "Error", 
                          MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(SelectedFilePath) && !string.IsNullOrWhiteSpace(LibraryName))
        {
         
            string filename = LibraryName;
            if (!filename.EndsWith(".db", StringComparison.OrdinalIgnoreCase))
            {
                filename += ".db";
            }
            SelectedFilePath = System.IO.Path.Combine(InitialDirectory, filename);
        }
        //DialogResult = true;
    }
}