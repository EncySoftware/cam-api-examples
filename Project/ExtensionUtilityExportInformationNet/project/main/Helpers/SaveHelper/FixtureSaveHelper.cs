using CAMAPI.Application;
using CAMAPI.CoordinateSystem;
using CAMAPI.DotnetHelper;
using CAMAPI.Extension.PLM;
using CAMAPI.GeomModel;
using CAMAPI.Machine;
using CAMAPI.ModelFormerTypes;
using CAMAPI.PartStage;
using CAMAPI.Project;
using CAMAPI.SurfaceTypes;
using CAMAPI.TechOperation;
using Geometry.VecMatrLib;

namespace ExtensionUtilityExportInformationNet;

/// <summary>Exports the fixtures assigned to the current part, including inherited instances.</summary>
public static class FixtureSaveHelper
{
    /// <summary>Writes one Fixtures entry per geometry-bearing node, in machine world coordinates.</summary>
    public static void SaveFixtures(JsonBuilder json,
        ComWrapper<ICamApiProject> project, ComWrapper<ICamApiApplication> application,
        ComWrapper<ICamApiPartStage> partStage, ComWrapper<ICamApiMachine> machine,
        ComWrapper<ICamApiMachineEvaluator> evaluator,
        ComWrapper<ICamApiListCoordinateSystem> coordinateSystems,
        ComWrapper<ICamApiFacesToTriangulatedFilesConverter> converter,
        int setupIndex, int partIndex)
    {
        json.BeginArray("Fixtures");
        using var operation = partStage.AsInstanceOf<ICamApiTechOperation>();
        if (operation == null || operation.IsNull)
        {
            json.EndArray();
            return;
        }
        operation.InitMachineEvaluator(evaluator);
        RefreshParentFixtures(operation);
        using var former = operation.ModelFormerFixtures();
        if (former.IsNull)
        {
            json.EndArray();
            return;
        }
        // Refresh inherited nodes; their aggregate face list can legitimately be empty.
        using var refresh = former.GetFaceList(T3DMatrix.Unit);
        using var setup = partStage.WorkpieceSetup();
        using var geometryCS = coordinateSystems.GetByName(setup.WorkpieceSideCoordinateSystemName());
        T3DMatrix partPlacement = (T3DMatrix)geometryCS.Matrix() * (T3DMatrix)setup.Offset();
        if (setup.MachineSideConnectorIndex() >= 0)
            partPlacement *= (T3DMatrix)evaluator.GetWorldWorkpieceConnectorMatrix(setup.MachineSideConnectorIndex());

        for (var i = former.LowItemIndex(); i <= former.TopItemIndex(); i++)
        {
            using var item = former.Item(i);
            using var connector = item.AsInstanceOf<ICamApiFixtureConnectorItem>();
            if (connector == null || connector.IsNull)
                continue;
            using var component = connector.GetComponent();
            if (component.IsNull)
                continue;
            using var componentItem = component.AsInstanceOf<ICamApiModelItem>();
            if (componentItem == null || componentItem.IsNull)
                throw new InvalidOperationException("Fixture export requires a CAM kernel with inherited fixture API support.");
            var connectorIndex = connector.ConnectorIndex() - 1;
            var connectorName = "";
            if (connectorIndex >= 0)
            {
                using var machineConnector = machine.WorkpieceConnector(connectorIndex);
                connectorName = machineConnector.Name();
            }
            T3DMatrix connectorGlobal = connector.GlobalMatrix();
            T3DMatrix connectorSetup = connector.SetupLCS();
            T3DMatrix world = connectorIndex >= 0 && !connector.DoNotUseConnectorMatrix()
                ? evaluator.GetWorldWorkpieceConnectorMatrix(connectorIndex) : partPlacement;
            // Remove the simulator-relative connector matrix before applying its world placement.
            var toWorld = connectorGlobal.InverseMatrix() * connectorSetup * world;
            for (var j = 0; j < component.NodeCount(); j++)
            {
                using var node = component.GetNode(j);
                Visit(node, connector.IsVisible() && componentItem.IsVisible(), 0);
            }

            void Visit(ComWrapper<ICamApiFixtureNodeItem> node, bool parentVisible, int depth)
            {
                if (depth > 32)
                    throw new InvalidOperationException("Fixture tree is too deep.");
                using var nodeItem = node.AsInstanceOf<ICamApiModelItem>();
                if (nodeItem == null || nodeItem.IsNull)
                    throw new InvalidOperationException("Fixture export requires a CAM kernel with inherited fixture API support.");
                var visible = parentVisible && nodeItem.IsVisible();
                var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (node.FaceCount() > 0)
                {
                    var id = nodeItem.XmlID().ToString("N");
                    json.BeginObject();
                    json.AddStrPair("Id", id);
                    json.AddStrPair("Caption", node.Caption());
                    json.AddStrPair("Component", component.Caption());
                    json.AddStrPair("ModelItemClassName", nodeItem.ClassName());
                    json.AddBoolPair("IsInherited", nodeItem.ClassName() == "TFxAxisInstanceItem");
                    json.AddBoolPair("IsVisible", visible);
                    json.AddIntPair("WorkpieceConnectorIndex", connectorIndex);
                    json.AddStrPair("WorkpieceConnectorName", connectorName);
                    json.AddBoolPair("DoNotUseConnectorMatrix", connector.DoNotUseConnectorMatrix());
                    json.AddIntPair("MultiplyCount", node.MultiplyCount());
                    json.AddStrPair("PlacementSpace", "MachineWorld");
                    GeometrySaveHelper.ShowMatrixData(node.SetupLCS(), "SetupLCS", json);
                    GeometrySaveHelper.ShowMatrixData(node.GlobalMatrix(), "NodeMatrix", json);
                    GeometrySaveHelper.ShowMatrixData(connectorSetup, "ConnectorSetupLCS", json);
                    GeometrySaveHelper.ShowMatrixData(world, "WorldConnectorMatrix", json);
                    GeometrySaveHelper.ShowMatrixData((T3DMatrix)node.GlobalMatrix() * toWorld, "WorldPlacementMatrix", json);
                    if (node.MultiplyCount() > 1)
                        json.AddStrPair("Warning", "WorldPlacementMatrix describes the base node; multiplied copies are not expanded.");

                    json.BeginArray("ModelItems");
                    for (var k = 0; k < node.FaceCount(); k++)
                    {
                        using var face = node.GetFaceItem(k);
                        if (face.IsNull)
                            continue;
                        json.BeginObject();
                        json.AddStrPair("Caption", face.Caption());
                        json.AddStrPair("ModelItemClassName", face.ClassName());
                        json.AddBoolPair("IsVisible", face.IsVisible());
                        using var geometry = face.AsInstanceOf<ICamApiGeometryNodeBasedModelItem>();
                        if (geometry != null && !geometry.IsNull)
                        {
                            var path = geometry.Invoke(g => g.GeometryNodeFullName);
                            json.AddStrPair("GeometryNodeFullName", path);
                            if (!string.IsNullOrWhiteSpace(path))
                            {
                                if (face.IsVisible()) paths.Add(path);
                                WriteSources(json, project, application, path);
                            }
                        }
                        json.EndObject();
                    }
                    json.EndArray();
                    if (visible && paths.Count > 0)
                    {
                        using var faces = GetAssignedFaces(project, paths);
                        json.AddIntPair("FaceCount", faces.IsNull ? 0 : faces.Count());
                        if (!faces.IsNull && faces.Count() > 0)
                        {
                            var name = $"fixture_{setupIndex}_{partIndex}_{id}.stl";
                            var folder = Path.Combine(ExportOutputPaths.Root, "Output");
                            Directory.CreateDirectory(folder);
                            converter.SetTolerance(0.01);
                            converter.SaveFacesToSTL(faces, Path.Combine(folder, name));
                            json.AddStrPair("GeometryType", "STL");
                            json.AddStrPair("GeometrySpace", "Source");
                            json.AddStrPair("FileName", name);
                        }
                    }
                    json.EndObject();
                }
                for (var j = 0; j < node.NodeCount(); j++)
                {
                    using var child = node.GetNode(j);
                    Visit(child, visible, depth + 1);
                }
            }
        }
        json.EndArray();
    }

    private static void RefreshParentFixtures(ComWrapper<ICamApiTechOperation> operation, int depth = 0)
    {
        if (depth > 32)
            throw new InvalidOperationException("Operation tree is too deep.");
        using var parent = operation.GetParentOperation(TCamApiReorderingMode.rmDesigned);
        if (parent.IsNull)
            return;
        RefreshParentFixtures(parent, depth + 1);
        using var former = parent.ModelFormerFixtures();
        if (!former.IsNull)
        {
            // A part inherits from its setup, whose root instances may not be refreshed yet.
            using var refresh = former.GetFaceList(T3DMatrix.Unit);
        }
    }
    private static void WriteSources(JsonBuilder json, ComWrapper<ICamApiProject> project,
        ComWrapper<ICamApiApplication> application, string assignedPath)
    {
        static string Normalize(string value) => value.Replace('/', '\\').Trim('\\');
        var path = Normalize(assignedPath);
        using var geometry = project.CAMAPIGeomModel();
        using var iterator = geometry.GetNodes();
        using var plm = application.AsInstanceOf<IPLMApplication>();
        json.BeginArray("Sources");
        foreach (var node in iterator.AsEnumerable())
        {
            var nodePath = Normalize(node.FullName());
            if (!string.Equals(path, nodePath, StringComparison.OrdinalIgnoreCase)
                && !path.StartsWith(nodePath + "\\", StringComparison.OrdinalIgnoreCase)
                && !nodePath.StartsWith(path + "\\", StringComparison.OrdinalIgnoreCase))
                continue;
            using var entity = node.GeometryEntity();
            using var imported = entity.AsInstanceOf<ICamApiImportedGeometryEntity>();
            if (imported == null || imported.IsNull)
                continue;
            var source = imported.CADModelLocalFileName() ?? string.Empty;
            var fromPlm = source.StartsWith("PLMGUID:", StringComparison.OrdinalIgnoreCase);
            json.BeginObject();
            json.AddStrPair("GeometryNodeFullName", nodePath);
            json.AddStrPair("SourceCADModelFileID", source);
            json.AddBoolPair("ImportedFromPLM", fromPlm);
            if (fromPlm)
            {
                json.AddStrPair("PLMObjectID", source);
                using var plmObject = plm == null || plm.IsNull
                    ? ComWrapper.Create<IPLMObjectInCAM>(null)
                    : plm.InvokeAndWrap(p => p.FindObjectByID(source));
                json.AddBoolPair("PLMObjectResolved", !plmObject.IsNull);
                if (!plmObject.IsNull)
                {
                    json.AddStrPair("IdInPLM", plmObject.Invoke(p => p.IdInPLM));
                    json.AddStrPair("NameInPLM", plmObject.Invoke(p => p.Name));
                    json.AddStrPair("ConnectionId", plmObject.Invoke(p => p.ConnectionId));
                }
            }
            json.EndObject();
        }
        json.EndArray();
    }

    private static ComWrapper<ICamApiFaceList> GetAssignedFaces(
        ComWrapper<ICamApiProject> project, IEnumerable<string> paths)
    {
        using var geometry = project.CAMAPIGeomModel();
        using var selection = new ListComWrapper<ICAMAPIGeometryTreeNode>();
        using (var iterator = geometry.GetNodes())
            foreach (var node in iterator.AsEnumerable())
                if (node.Selected()) selection.Add(node);
        try
        {
            geometry.DeselectAll();
            foreach (var path in paths)
            {
                using var node = geometry.FindByFullName(path);
                SelectFaces(node);
            }
            return geometry.GetFaceListOfSelected();
        }
        finally
        {
            geometry.DeselectAll();
            foreach (var node in selection) node.SetSelected(true);
        }
        static void SelectFaces(ComWrapper<ICAMAPIGeometryTreeNode> node)
        {
            using var entity = node.GeometryEntity();
            if (entity.EntityType() is TCAMAPIGeometryEntityType.etFace or TCAMAPIGeometryEntityType.etMesh)
                node.SetSelected(true);
            foreach (var child in node.EnumerateChildren()) SelectFaces(child);
        }
    }
}
