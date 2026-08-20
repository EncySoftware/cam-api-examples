namespace ProjectInfoViewer;

/// <summary>Body of POST /api/activate: path to the json the running viewer should display.</summary>
public record ActivateRequest(string JsonPath);
