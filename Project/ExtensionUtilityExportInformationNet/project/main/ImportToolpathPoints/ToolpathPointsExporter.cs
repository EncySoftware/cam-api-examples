using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using STGeomApiTypes;
using STTypes;

namespace ExtensionUtilityExportInformationNet;

/// <summary>
/// Reads JSON toolpath files and writes points into SGF via ISTGeomReceiver
/// </summary>
public static class ToolpathPointsExporter
{
    private const int DefaultColor = 7554080; // #5FAFFF — ffWorking

    private static readonly Dictionary<int, int> FeedTypeColors = new()
    {
        { 0,    7554080 },  // ffWorking          → #206cb7 → BGR $b76c20
        { 1,    2500334 },   // ffRapid            → #EE2626 → BGR $2626EE
        { 2,    8060557 },   // ffFirst            → #8DFF7A → BGR $7AFF8D
        { 4,    5106431 },     // ffEngage           → #FFEA7A → BGR $009999
        { 8,    5106431 },  // ffRetract          → #FFEA7A → BGR $009999
        { 16,   153 },       // ffPlunge           → #990000 → BGR $000099
        { 32,   16701791 },  // ffFinish           → #5FD7FF → BGR $FFD75F
        { 64,   1441658 },   // ffNext             → #7AFF15 → BGR $15FF7A
        { 128,  15891199 },  // ffReturn           → #FF7AF2 → BGR $F27AFF
        { 256,  2259694 },   // ffApproach         → #EE7A22 → BGR $227AEE
        { 512,  2500334 },   // ffRapid5D          → #EE2626 → BGR $2626EE
        { 1024, 2500334 },   // ffTransitionOnSafe → #EE2626 → BGR $2626EE
        { 2048, 2500334 },   // ffApproachFromSafe → #EE2626 → BGR $2626EE
        { 4096, 2500334 },   // ffReturnToSafe     → #EE2626 → BGR $2626EE
        { 8192, 5274367 },  // ffLongNext         → #FF7A80 → BGR $807AFF
    };

    private static int GetColorForFeedType(int feedType) =>
        FeedTypeColors.GetValueOrDefault(feedType, DefaultColor);


    private static TST3DPoint ToTST3DPoint(Point3D p) => new() { X = p.X, Y = p.Y, Z = p.Z };
    
    ///
    public static void ProcessJsonFile(ISTGeomReceiver geomFile, string jsonFilePath)
    {
        var jsonContent = File.ReadAllText(jsonFilePath);
        var toolpathRoot = JsonSerializer.Deserialize<ToolpathRoot>(jsonContent)
                           ?? throw new Exception("Failed to deserialize: " + jsonFilePath);

        var fileName = Path.GetFileNameWithoutExtension(jsonFilePath);

        // Group per JSON file
        geomFile.StartGroupEntity(fileName);
        try
        {
            int groupIndex = 0;
            int currentFeedType = 0;
            foreach (var command in toolpathRoot.CAMToolpath.Commands)
            {
                if (command.FeedType.HasValue)
                    currentFeedType = command.FeedType.Value;
                
                if (command.Points == null || command.Points.Count == 0)
                    continue;

                var groupName = !string.IsNullOrEmpty(command.CommandCaption)
                    ? command.CommandCaption
                    : $"Command_{command.CommandCode}";

                geomFile.StartGroupEntity(groupName);
                Debug.Print($"feedtype {currentFeedType}");
                geomFile.SetCurrentColor(GetColorForFeedType(currentFeedType));
                //geomFile.SetCurrentLineType(currentFeedType == 1 ? TSTLineType.ltDash : TSTLineType.ltSolid);
                try
                {
                    for (int i = 0; i < command.Points.Count - 1; i++)
                    {
                        var sp = ToTST3DPoint(command.Points[i].EndPoint);
                        var tp = ToTST3DPoint(command.Points[i + 1].EndPoint);
                        var segId = $"{fileName}_{groupIndex}_seg{i}";
                        geomFile.CreateLineSeg(segId, sp, tp);
                        geomFile.AddEntity(segId, command.CommandCaption);
                    }
                }
                finally
                {
                    geomFile.CloseGroupEntity();
                }

                groupIndex++;
            }
        }
        finally
        {
            geomFile.CloseGroupEntity();
        }
    }
}
