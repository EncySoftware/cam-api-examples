using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ExtensionOperationFlipToolNet;

/// <summary>
/// Simple window to ask user for text input
/// </summary>
public partial class TextInputWindow
{
    private readonly List<AxesInfo> _axes;
    private readonly Dictionary<string, TextBox> _axesTextboxes = new();

    public ToolParameters Settings { get; private set; }
    /// <summary>
    /// User input from the text box
    /// </summary>

    /// <summary>
    /// Simple window to ask user for text input
    /// </summary>
    public TextInputWindow(List<AxesInfo> axes)
    {
        _axes = axes;
        
        InitializeComponent();
        Settings = new ToolParameters();
        CreateDynamicControls();
    }

    private void CreateDynamicControls()
    {
        // Create axes inputs
        foreach (var axes in _axes)
        {
            var stackPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 5) };
            
            var label = new TextBlock
            {
                Text = $"(ID: {axes.Id}):",
                Width = 120,
                Margin = new Thickness(0, 0, 10, 0)
            };
            
            var textBox = new TextBox
            {
                Text = axes.Value.ToString(),
                Width = 80,
                IsEnabled = axes.Enabled,
                Tag = axes.Id
            };
            
            stackPanel.Children.Add(label);
            stackPanel.Children.Add(textBox);
            AxesPanel.Children.Add(stackPanel);
            _axesTextboxes[axes.Id] = textBox;
        }

        // Update headers with counts
        AxesHeader.Text = $"Axes Values ({_axes.Count} items):";
    }
    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        Settings.Axes.Clear();

        // Get axes values and add to Axes list
        foreach (var axes in _axes)
        {
            if (_axesTextboxes.TryGetValue(axes.Id, out var textBox))
            {
                Settings.Axes.Add(new AxesInfo 
                { 
                    Id = axes.Id, 
                    Enabled = axes.Enabled, 
                    Value = ParseDouble(textBox.Text) 
                });
            }
        }

        // Get XYZ values
        DialogResult = true;
        Close();
    }

    private double ParseDouble(string text)
    {
        return double.TryParse(text, out double result) ? result : 0;
    }
}