using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ExtensionOperationFlipToolNet;

/// <summary>
/// Simple window to ask user for text input
/// </summary>
public partial class InvTextInputWindow
{
    private readonly List<FlipInfo> _flips;
    private readonly Dictionary<string, CheckBox> _flipCheckboxes = new();

    public InvToolParameters SettingsInv { get; private set; }
    /// <summary>
    /// User input from the text box
    /// </summary>

    /// <summary>
    /// Simple window to ask user for text input
    /// </summary>
    public InvTextInputWindow(List<FlipInfo> flips)
    {
        _flips = flips;

        InitializeComponent();
        SettingsInv = new InvToolParameters();
        CreateDynamicControlsInv();
        
        NormalComboBox.SelectedIndex = 0;
        UpdateXYZFromNormal(0);
    }

    private void CreateDynamicControlsInv()
    {
        // Create flip checkboxes
        foreach (var flip in _flips)
        {
            var checkBox = new CheckBox
            {
                Content = $"{flip.Caption} (ID: {flip.Id})",
                IsChecked = flip.Enabled,
                Margin = new Thickness(0, 0, 0, 5),
                Tag = flip.Id
            };
            
            FlipsPanel.Children.Add(checkBox);
            _flipCheckboxes[flip.Id] = checkBox;
        }


        // Update headers with counts
        FlipsHeader.Text = $"Flip Options ({_flips.Count} items):";
    
    }
    private void Ok_ClickInv(object sender, RoutedEventArgs e)
    {
        SettingsInv.Flips.Clear();
        

        // Get flip values and add to Flips list
        foreach (var flip in _flips)
        {
            if (_flipCheckboxes.TryGetValue(flip.Id, out var checkBox))
            {
                SettingsInv.Flips.Add(new FlipInfo 
                { 
                    Id = flip.Id, 
                    Caption = flip.Caption, 
                    Enabled = checkBox.IsChecked ?? false 
                });
            }
        }

        SettingsInv.X = ParseDoubleInv(XTextBox.Text);
        SettingsInv.Y = ParseDoubleInv(YTextBox.Text);
        SettingsInv.Z = ParseDoubleInv(ZTextBox.Text);

        // Get XYZ values
        DialogResult = true;
        Close();
    }

    private void NormalComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (NormalComboBox.SelectedIndex >= 0)
        {
            UpdateXYZFromNormal(NormalComboBox.SelectedIndex);
        }
    }

    private void UpdateXYZFromNormal(int selectedIndex)
    {
        switch (selectedIndex)
        {
            case 0: // Up (Z = 1)
                XTextBox.Text = "0";
                YTextBox.Text = "0";
                ZTextBox.Text = "1";
                break;
            case 1: // Down (Z = -1)
                XTextBox.Text = "0";
                YTextBox.Text = "0";
                ZTextBox.Text = "-1";
                break;
            case 2: // Forward (Y = -1)
                XTextBox.Text = "0";
                YTextBox.Text = "-1";
                ZTextBox.Text = "0";
                break;
            case 3: // Backward (Y = 1)
                XTextBox.Text = "0";
                YTextBox.Text = "1";
                ZTextBox.Text = "0";
                break;
            case 4: // Left (X = -1)
                XTextBox.Text = "-1";
                YTextBox.Text = "0";
                ZTextBox.Text = "0";
                break;
            case 5: // Right (X = 1)
                XTextBox.Text = "1";
                YTextBox.Text = "0";
                ZTextBox.Text = "0";
                break;
        }
    }
    private double ParseDoubleInv(string text)
    {
        return double.TryParse(text, out double result) ? result : 0;
    }
}