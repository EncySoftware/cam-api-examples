using System.Windows;
using Microsoft.Win32;
using System.Collections.ObjectModel;
namespace ExtensionOperationToolPopupNet;

/// <summary>
/// Simple window to ask user for text input
/// </summary>
public partial class TextInputWindow
{
    /// <summary>
    /// Name of library
    /// </summary>
    public string? LibraryName { get; private set; }
    /// <summary>
    /// Tool id
    /// </summary>
    public string? ToolId { get; private set; }
    /// <summary>
    /// Action to add tool(LibraryName, ToolId for args)
    /// </summary>
    public event Action<string, string>? OnToolAdded;
    private ObservableCollection<string> itemsCollection;
    /// <summary>
    /// Simple window to ask user for text input
    /// </summary>
    public TextInputWindow(List<string> items)
    {
        InitializeComponent();
        itemsCollection = new ObservableCollection<string>(items);
        ItemsListBox.ItemsSource = itemsCollection;
    }
    /// <summary>
    /// Updating list of items after adding new element
    /// </summary>
    public void UpdateItems(List<string> newItems)
    {
        Dispatcher.Invoke(() =>
        {
            itemsCollection.Clear();
            foreach (var item in newItems)
            {
                itemsCollection.Add(item);
            }
            
            NameTextBox.Text = "";
            IdTextBox.Text = "";
        });
    }
    private void Import_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = "Database files (*.db)|*.db|All files (*.*)|*.*",
            FilterIndex = 1,
            Title = "Select database file"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            string selectedPath = openFileDialog.FileName; 
            string folderName = System.IO.Path.GetFileName(selectedPath); 
            NameTextBox.Text = folderName; 
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

        OnToolAdded?.Invoke(LibraryName, ToolId);
    }
    /// <summary>
    /// Message if it couldn`t find tool with exact id
    /// </summary>
    public void HandleToolNotFound(string toolId, string filePath)
    {
        MessageBox.Show($"Tool with Id: {toolId} not found in {filePath}", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
    }
}