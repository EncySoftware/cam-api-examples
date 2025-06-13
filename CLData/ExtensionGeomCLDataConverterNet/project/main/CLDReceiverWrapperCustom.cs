using CAMAPI.DotnetHelper;
using CAMAPI.Extensions;
using CAMAPI.MCDFormerTypes;
using STTypes;

namespace ExtensionGeomCLDataConverterNet;

/// <summary>
/// Simple realization just to log commands
/// </summary>
public class CLDReceiverWrapperCustom : CLDRecevierWrapperDefault
{
    /// <summary>
    /// Value to increase Z-coordinate of points in CLData
    /// </summary>
    public static int IncreaseZValue = 20;
    
    private int _currentFeed = (int)TFeedTypeFlag.affWorking;
    
    /// <summary>
    /// Simple realization just to log commands
    /// </summary>
    public CLDReceiverWrapperCustom(ICamApiCLDReceiver receiver) : base(receiver) { }
    
    public override void CutTo(TST3DPoint p)
    {
        if (_currentFeed == (int)TFeedTypeFlag.affWorking)
            p.Z += IncreaseZValue;
        base.CutTo(p);
    }
    
    public override void OutStandardFeed(int feed)
    {
        _currentFeed = feed;
        base.OutStandardFeed(feed);
    }
    
    public override void OutPercentFeed(int feed, double percent)
    {
        _currentFeed = feed;
        base.OutPercentFeed(feed, percent);
    }
    
    public override void OutFeed(int feed, double value, bool mpm)
    {
        _currentFeed = feed;
        base.OutFeed(feed, value, mpm);
    }
    
    public override void ArcTo2d(TST3DPoint pe, TST3DPoint pc, TCLDPlaneType plane, double rc, bool canBeFull)
    {
        if (_currentFeed == (int)TFeedTypeFlag.affWorking)
        {
            pe.Z += IncreaseZValue;
            pc.Z += IncreaseZValue;
        }
        base.ArcTo2d(pe, pc, plane, rc, canBeFull);
    }
}