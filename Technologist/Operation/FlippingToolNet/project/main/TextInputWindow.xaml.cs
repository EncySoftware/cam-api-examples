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
    private readonly Dictionary<string, TextBox> _axesTextboxes = [];
    private readonly Dictionary<string, CheckBox> _axesCheckboxes = [];
    /// <summary>
    /// Tool params
    /// </summary>
    public event Action<ToolParameters>? SettingsApplied;
    /// <summary>
    /// Tool params
    /// </summary>
    public ToolParameters Settings { get; private set; }
    

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
        // Create header for axes section
        var headerPanel = new StackPanel 
        { 
            Orientation = Orientation.Horizontal, 
            Margin = new Thickness(0, 0, 0, 10) 
        };
        
        var axisHeader = new TextBlock 
        { 
            Text = "Axis Values", 
            FontWeight = FontWeights.Bold,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 10, 0)
        };
        
        var valueHeader = new TextBlock 
        { 
            Text = "Value", 
            Width = 120,
            FontWeight = FontWeights.Bold,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 10, 0)
        };
        
        var enableHeader = new TextBlock 
        { 
            Text = "Enable", 
            Width = 60, 
            FontWeight = FontWeights.Bold,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(10, 0, 0, 0)
        };

        headerPanel.Children.Add(axisHeader);
        headerPanel.Children.Add(valueHeader);
        headerPanel.Children.Add(enableHeader);
        AxesPanel.Children.Add(headerPanel);


        // Create axes inputs
        foreach (var axes in _axes)
        {
            var stackPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 5) };
            
            // Checkbox for enabling/disabling the axis
            var checkBox = new CheckBox
            {
                IsChecked = axes.Defined,
                Width = 80,
                Height = 20,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                Tag = axes.Id,
                Margin = new Thickness(0, 0, 10, 0)
            };

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
                IsEnabled = axes.Defined,
                Tag = axes.Id
            };

            checkBox.Checked += (s, e) => textBox.IsEnabled = true;
            checkBox.Unchecked += (s, e) => textBox.IsEnabled = false;
            
            Settings.AxesIds.Add(axes.Id);
            
            stackPanel.Children.Add(label);
            stackPanel.Children.Add(textBox);
            stackPanel.Children.Add(checkBox);
            AxesPanel.Children.Add(stackPanel);
            _axesTextboxes[axes.Id] = textBox;
            _axesCheckboxes[axes.Id] = checkBox;
        }

        // Update headers with counts
        AxesHeader.Text = $"Axes Values ({_axes.Count} items):";
    }
    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        UpdateSettings();

        // Get XYZ values
        DialogResult = true;
        Close();
    }
    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        UpdateSettings();
        SettingsApplied?.Invoke(Settings);
    }

    private void UpdateSettings()
    {
        Settings.Axes.Clear();

        // Get axes values and add to Axes list
        foreach (var axes in _axes)
        {
            if (_axesTextboxes.TryGetValue(axes.Id, out var textBox) && 
                _axesCheckboxes.TryGetValue(axes.Id, out var checkBox) )
            {
                Settings.Axes.Add(new AxesInfo 
                { 
                    Id = axes.Id, 
                    Defined = checkBox.IsChecked ?? false,
                    Value = ParseDouble(textBox.Text) 
                });
            }
        }
    }

    private double ParseDouble(string text)
    {
        return double.TryParse(text, out double result) ? result : 0;
    }
}