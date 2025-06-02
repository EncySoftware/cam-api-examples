using System.Drawing;
using System.Globalization;
using System.Text.RegularExpressions;
using STTypes;
using Svg;
using Svg.Pathing;

namespace AddinImportSvgNet;

/// <summary>
/// Addin to import from SVG. Calls callbacks
/// </summary>
public class SvgToCamConverter
{
    private static int _entityId;
    
    private void NotifyError(string message)
    {
        //
    }

    /// <summary>
    /// Utility to import from SVG. Calls callbacks
    /// </summary>
    public void ImportSvg(string filePath, SvgReaderCallbacks callbacks)
    {
        var doc = SvgDocument.Open<SvgDocument>(filePath);

        foreach (var element in doc.Children)
        {
            try
            {
                ProcessElement(element, callbacks);
            } catch (Exception ex)
            {
                NotifyError($"Error processing element: {ex.Message}");
            }
        }
    }
    
    private static string CreateId(string type)
    {
        _entityId++;
        return $"{type}_{_entityId}";
    }
    
    private static void SetLineParams(SvgElement element, SvgReaderCallbacks callbacks)
    {
        SetLineColor(element, callbacks);
    }

    private static void SetLineColor(SvgElement element, SvgReaderCallbacks callbacks)
    {
        if (element.Stroke is not SvgColourServer colourServer)
        {
            callbacks.OnSetLineColor?.Invoke(0);
            return;
        }
        
        var colour = colourServer.Colour;
        var colorInt = (colour.R << 16) | (colour.G << 8) | colour.B;
        callbacks.OnSetLineColor?.Invoke(colorInt);
    }

    private void ProcessElement(SvgElement element, SvgReaderCallbacks callbacks)
    {
        switch (element)
        {
            case SvgPath pathElement:
                ParsePath(pathElement.PathData, callbacks);
                break;

            case SvgLine line:
            {
                var start = To3D(line.StartX, line.StartY);
                var end = To3D(line.EndX, line.EndY);

                var id = CreateId("line");
                SetLineParams(line, callbacks);
                callbacks.OnMoveTo?.Invoke(id, start);
                callbacks.OnLineTo?.Invoke(end);
                callbacks.OnClosePath?.Invoke(true);
            }
                // не замыкаем
                break;
            
            case SvgRectangle rect:
            {
                var x = rect.X.ToDeviceValue(null, UnitRenderingType.Horizontal, null);
                var y = rect.Y.ToDeviceValue(null, UnitRenderingType.Vertical, null);
                var width = rect.Width.ToDeviceValue(null, UnitRenderingType.Horizontal, null);
                var height = rect.Height.ToDeviceValue(null, UnitRenderingType.Vertical, null);

                var p1 = To3D(x, y);
                var p2 = To3D(x + width, y);
                var p3 = To3D(x + width, y + height);
                var p4 = To3D(x, y + height);
                
                var id = CreateId("rect");
                SetLineParams(rect, callbacks);
                callbacks.OnMoveTo?.Invoke(id, p1);
                callbacks.OnLineTo?.Invoke(p2);
                callbacks.OnLineTo?.Invoke(p3);
                callbacks.OnLineTo?.Invoke(p4);
                callbacks.OnClosePath?.Invoke(true);
            }
                break;

            case SvgCircle circle:
            {
                var id = CreateId("circle");
                var radius = circle.Radius.ToDeviceValue(null, UnitRenderingType.Other, null);
                var centerX = circle.CenterX.ToDeviceValue(null, UnitRenderingType.Horizontal, null);
                var centerY = circle.CenterY.ToDeviceValue(null, UnitRenderingType.Vertical, null);
                callbacks.OnCircleTo?.Invoke(id, radius, centerX, centerY);
            }
                break;

            case SvgEllipse ellipse:
            {
                // get base params
                var cx = ellipse.CenterX.ToDeviceValue(null, UnitRenderingType.Horizontal, null);
                var cy = ellipse.CenterY.ToDeviceValue(null, UnitRenderingType.Vertical, null);
                var rx = ellipse.RadiusX.ToDeviceValue(null, UnitRenderingType.Horizontal, null);
                var ry = ellipse.RadiusY.ToDeviceValue(null, UnitRenderingType.Vertical, null);

                // get rotation
                var id = CreateId("ellipse");
                var rotationDegrees = 0.0;
                if (ellipse.TryGetAttribute("transform", out var transformString))
                {
                    var match = Regex.Match(transformString, @"rotate\(([-\d.]+)(?:[, ]+)([-\d.]+)?(?:[, ]+)?([-\d.]+)?\)");
                    if (match.Success)
                        rotationDegrees = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                }
                
                // draw
                callbacks.OnEllipseTo?.Invoke(id, rx, ry, cx, cy, rotationDegrees);
            }
                break;
            
            case SvgPolygon polygon:
            {
                var rawPoints = polygon.Points;
                if (rawPoints.Count < 4)
                    break;
                if (rawPoints.Count % 2 != 0)
                    throw new Exception("Invalid number of points in polygon");

                List<TST3DPoint> points = [];
                for (var i = 0; i < rawPoints.Count; i += 2)
                {
                    var x = rawPoints[i].ToDeviceValue(null, UnitRenderingType.Horizontal, null);
                    var y = rawPoints[i + 1].ToDeviceValue(null, UnitRenderingType.Vertical, null);
                    points.Add(To3D(x, y));
                }

                if (points.Count > 1)
                {
                    var id = CreateId("polygon");
                    SetLineParams(polygon, callbacks);
                    callbacks.OnMoveTo?.Invoke(id, points[0]);
                    foreach (var point in points.Skip(1))
                        callbacks.OnLineTo?.Invoke(point);
                    callbacks.OnClosePath?.Invoke(true);
                }
            }
                break;
            
            default:
                NotifyError($"Unsupported SVG element: {element.GetType().Name}");
                break;
        }

        // recursive call for child elements
        foreach (var child in element.Children)
            ProcessElement(child, callbacks);
    }
    
    private void ParsePath(SvgPathSegmentList pathData, SvgReaderCallbacks callbacks)
    {
        TST3DPoint? startPoint = null;
        
        var segments = pathData.ToList();
        if (segments.Count == 0)
            return;
        
        // start point
        var startSegment = segments[0];
        if (startSegment is SvgMoveToSegment moveSegment)
        {
            var id = CreateId("path");
            startPoint = To3D(moveSegment.End);
            callbacks.OnMoveTo?.Invoke(id, startPoint.Value);
        }
        
        for(var i = 1; i < segments.Count; i++)
        {
            var segment = segments[i];
            switch (segment)
            {
                case SvgMoveToSegment move:
                    var moveTo = To3D(move.End);
                    startPoint = moveTo;
                    break;

                case SvgLineSegment line:
                    var lineTo = To3D(line.End);
                    callbacks.OnLineTo?.Invoke(lineTo);
                    break;

                case SvgClosePathSegment:
                    if (startPoint.HasValue)
                        callbacks.OnLineTo?.Invoke(startPoint.Value);
                    callbacks.OnClosePath?.Invoke(true);
                    break;
                
                default:
                    NotifyError($"Unsupported SVG path segment: {segment.GetType().Name}");
                    break;
            }
        }
    }

    private TST3DPoint To3D(PointF pt) 
    {
        return new TST3DPoint
        {
            X = pt.X,
            Y = pt.Y,
            Z = 0.0
        };
    }
    
    private TST3DPoint To3D(double x, double y) 
    {
        return new TST3DPoint
        {
            X = x,
            Y = y,
            Z = 0.0
        };
    }
}