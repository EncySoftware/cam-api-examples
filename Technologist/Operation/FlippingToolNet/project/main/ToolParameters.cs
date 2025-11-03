using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ExtensionOperationFlipToolNet
{
    /// <summary>
    /// A class for tool parameters
    /// </summary>
    public partial class ToolParameters
    {
        public ToolParameters()
        {
            Axes = new List<AxesInfo>();
        }
        public List<AxesInfo> Axes { get; set; }
    }

    public partial class AxesInfo
    {
        public AxesInfo()
        {
            Id = string.Empty;
        }
        public string Id { get; set; }
        public bool Enabled { get; set; }
        public double Value { get; set; }
    }

}