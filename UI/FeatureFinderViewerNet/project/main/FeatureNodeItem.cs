using CAMAPI.FeatureFinder;

namespace FeatureFinderViewerNet;

/// <summary>
/// Plain-data record representing one feature found on a geometry node.
/// NodeName is the geometry tree full name used for viewport selection.
/// EntityNames are display names of the feature's base geometry entities.
/// </summary>
public record FeatureNodeItem(
    string Caption,
    TCamApiFeatureType FeatureType,
    string KeyInfo,
    IReadOnlyList<string> EntityPaths,
    List<PropertyItem> Properties
);

public record PropertyItem(string Name, string Value);
