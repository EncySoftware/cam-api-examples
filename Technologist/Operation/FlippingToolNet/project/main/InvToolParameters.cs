using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ExtensionOperationFlipToolNet
{

    /// <summary>
    /// A class for tool parameters
    /// </summary>
    public partial class InvToolParameters
    {
        public InvToolParameters()
        {
            Flips = new List<FlipInfo>();

        }
        public List<FlipInfo> Flips { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
       
    }
    public partial class FlipInfo
    {
        public FlipInfo()
        {
            Id = string.Empty;
            Caption = string.Empty;
        }
        public string Id { get; set; }
        public string Caption { get; set; }
        public bool Enabled { get; set; }
    }
}