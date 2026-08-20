using CAMAPI.Application;
using CAMAPI.DotnetHelper;
using CAMAPI.FeatureFinder;

namespace FeatureFinderViewerNet.Services;

class FeatureScanService : IFeatureScanService
{
    private readonly ComWrapper<ICamApiApplication> _appCom;

    public FeatureScanService(ComWrapper<ICamApiApplication> appCom)
    {
        _appCom = appCom;
    }

    public Task<IReadOnlyList<FeatureNodeItem>> ScanAsync()
        => Task.Run(Scan);

    private IReadOnlyList<FeatureNodeItem> Scan()
    {
        using var projectCom = _appCom.GetActiveProject();
        if (projectCom.IsNull)
            throw new Exception("No active project");

        using var ffCom = projectCom.FeatureFinder();
        
        // Start recognition in background and poll until finished.
        ffCom.RunRecognition(waitForCompletion: false);
        while (ffCom.IsUpdating())
            Thread.Sleep(200);

        using var geomModelCom = projectCom.CAMAPIGeomModel();

        var collected = new List<(string DedupeKey, FeatureNodeItem Item)>();

        foreach (var nodeCom in geomModelCom.EnumerateNodes())
        {
            using var childCom = nodeCom.Child();
            if (!childCom.IsNull)
                continue;

            var leafPath = nodeCom.FullName();
            using var featureListCom = ffCom.GetFeaturesForNode(leafPath);
            if (featureListCom.IsNull)
                continue;

            foreach (var featureCom in featureListCom.Enumerate())
            {
                var featureType = featureCom.FeatureType();
                var caption = featureCom.Caption();
                var entityNames = featureCom.BaseEntityNames();
                var dedupeKey = MakeDedupeKey(featureType, "", entityNames);

                var item = new FeatureNodeItem(caption, featureType, "", [leafPath], new List<PropertyItem>());
                collected.Add((dedupeKey, item));
            }
        }

        // Phase 2: merge entries with identical type + entity set.
        var merged = new Dictionary<string, FeatureNodeItem>();
        foreach (var (key, item) in collected)
        {
            if (!merged.TryGetValue(key, out var existing))
            {
                merged[key] = item;
            }
            else
            {
                var combined = new List<string>(existing.EntityPaths);
                foreach (var path in item.EntityPaths)
                {
                    if (!combined.Contains(path))
                        combined.Add(path);
                }
                merged[key] = existing with { EntityPaths = combined };
            }
        }

        return merged.Values.ToList();
    }

    private static string MakeDedupeKey(TCamApiFeatureType featureType, string keyInfo, IReadOnlyList<string> entityNames)
    {
        var sorted = string.Join(",", entityNames.OrderBy(x => x));
        return $"{featureType}|{keyInfo}|{sorted}";
    }

    private static string GetKeyInfo(ComWrapper<ICamApiFeature> featureCom)
    {
        var result = "";
        featureCom.Invoke(f =>
        {
            if (f is ICamApiHoleFeature hole)
            {
                result = $"Ø{hole.Diameter:F2}";
                return;
            }
            if (f is ICamApiFilletFeature fillet)
            {
                result = $"R{fillet.Size:F2}";
                return;
            }
            if (f is ICamApiChamferFeature chamfer)
            {
                result = $"C{chamfer.Size:F2}";
                return;
            }
            if (f is ICamApiPocketFeature pocket)
            {
                result = $"H{pocket.Height:F2}";
            }
        });
        return result;
    }

    private static List<PropertyItem> BuildProperties(ComWrapper<ICamApiFeature> featureCom)
    {
        var props = new List<PropertyItem>();

        props.Add(new PropertyItem("Valid", featureCom.IsValid() ? "Yes" : "No"));
        props.Add(new PropertyItem("Machined", featureCom.IsMachined() ? "Yes" : "No"));
        props.Add(new PropertyItem("Status", featureCom.Status().ToString()));
        props.Add(new PropertyItem("Z Min", $"{featureCom.ZMin():F3}"));
        props.Add(new PropertyItem("Z Max", $"{featureCom.ZMax():F3}"));

        var lcs = featureCom.Lcs();
        props.Add(new PropertyItem("Position", $"({lcs.vT.X:F3},  {lcs.vT.Y:F3},  {lcs.vT.Z:F3})"));
        props.Add(new PropertyItem("Axis Z", $"({lcs.vZ.X:F3},  {lcs.vZ.Y:F3},  {lcs.vZ.Z:F3})"));

        var subCount = featureCom.SubFeatureCount();
        if (subCount > 0)
            props.Add(new PropertyItem("Sub-features", subCount.ToString()));

        featureCom.Invoke(f =>
        {
            if (f is ICamApiHoleFeature hole)
            {
                props.Add(new PropertyItem("Diameter", $"{hole.Diameter:F3}"));
                props.Add(new PropertyItem("Depth", $"{hole.Height:F3}"));
                return;
            }
            if (f is ICamApiFilletFeature fillet)
            {
                props.Add(new PropertyItem("Radius", $"{fillet.Size:F3}"));
                return;
            }
            if (f is ICamApiChamferFeature chamfer)
            {
                props.Add(new PropertyItem("Size", $"{chamfer.Size:F3}"));
                return;
            }
            if (f is ICamApiPocketFeature pocket)
            {
                props.Add(new PropertyItem("Depth", $"{pocket.Height:F3}"));
            }
        });

        return props;
    }
}
