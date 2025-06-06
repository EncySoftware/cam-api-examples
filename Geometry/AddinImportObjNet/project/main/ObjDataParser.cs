using System.Globalization;

namespace AddinImportObjNet;

/// <summary>
/// Parses .obj files and extracts model data
/// </summary>
public static class ObjDataParser
{
    /// <summary>
    /// Parses .obj files and extracts model data
    /// </summary>
    public static ObjModel Parse(string filePath)
    {
        var model = new ObjModel();
        
        using var reader = new StreamReader(filePath);
        string? line;
        int lineNumber = 0;

        while ((line = reader.ReadLine()) != null)
        {
            lineNumber++;
            line = line.Trim();
            
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                continue;

            try
            {
                ProcessLine(line, model);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error parsing line {lineNumber}: {line}", ex);
            }
        }

        return model;
    }
    
    private static void ProcessLine(string line, ObjModel model)
    {
        var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return;

        switch (parts[0])
        {
            case "v":
                ParseVertex(parts, model.Vertices);
                break;
            case "vn":
                ParseNormal(parts, model.Normals);
                break;
            case "vt":
                ParseTextureCoord(parts, model.TextureCoordinates);
                break;
            case "f":
                ParseFace(parts, model.Faces);
                break;
        }
    }

    private static void ParseVertex(string[] parts, List<Vector3D> vertices)
    {
        if (parts.Length < 4) throw new FormatException("Vertex requires at least 3 coordinates");
        
        float x = float.Parse(parts[1], CultureInfo.InvariantCulture);
        float y = float.Parse(parts[2], CultureInfo.InvariantCulture);
        float z = float.Parse(parts[3], CultureInfo.InvariantCulture);
        
        vertices.Add(new Vector3D(x, y, z));
    }

    private static void ParseNormal(string[] parts, List<Vector3D> normals)
    {
        if (parts.Length < 4) throw new FormatException("Normal requires 3 coordinates");
        
        float x = float.Parse(parts[1], CultureInfo.InvariantCulture);
        float y = float.Parse(parts[2], CultureInfo.InvariantCulture);
        float z = float.Parse(parts[3], CultureInfo.InvariantCulture);
        
        normals.Add(new Vector3D(x, y, z));
    }

    private static void ParseTextureCoord(string[] parts, List<TextureCoordinate> textureCoords)
    {
        if (parts.Length < 2) throw new FormatException("Texture coordinate requires at least 1 coordinate");
        
        float u = float.Parse(parts[1], CultureInfo.InvariantCulture);
        float v = parts.Length > 2 ? float.Parse(parts[2], CultureInfo.InvariantCulture) : 0f;
        
        textureCoords.Add(new TextureCoordinate(u, v));
    }

    private static void ParseFace(string[] parts, List<Face> faces)
    {
        if (parts.Length < 4) throw new FormatException("Face requires at least 3 vertices");
        
        var face = new Face();


        for (int i = 1; i < parts.Length; i++)
        {
            var indices = parts[i].Split('/');
            var vertexIndices = new VertexIndices();

            vertexIndices.VertexIndex = int.Parse(indices[0]) - 1;

            if (indices.Length > 1 && !string.IsNullOrEmpty(indices[1]))
                vertexIndices.TextureCoordIndex = int.Parse(indices[1]) - 1;

            if (indices.Length > 2 && !string.IsNullOrEmpty(indices[2]))
                vertexIndices.NormalIndex = int.Parse(indices[2]) - 1;

            face.VertexIndices.Add(vertexIndices);
        }
        
        faces.Add(face);
    }
}