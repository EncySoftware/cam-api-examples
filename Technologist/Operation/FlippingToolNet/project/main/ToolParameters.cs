using System.Diagnostics.Metrics;
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
            AxesIds = new HashSet<string>();
        }
        public List<AxesInfo> Axes { get; set; }
        public HashSet<string> AxesIds { get; set; }
    }

    public partial class AxesInfo
    {
        public AxesInfo()
        {
            Id = string.Empty;
        }
        public string Id { get; set; }
        public bool Defined { get; set; }
        public double Value { get; set; }
    }

}