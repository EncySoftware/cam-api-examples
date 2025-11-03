using CAMAPI.Application;
using CAMAPI.DotnetHelper;
using CAMAPI.Extensions;
using CAMAPI.GeomModel;
using CAMAPI.ResultStatus;
using CAMAPI.UIDialogs;
using CAMAPI.UIDialogs.DotnetHelper;

namespace UtilityGeometryEntityReader;

/// <summary>
/// Utility to read information about selected model item
/// </summary>
public class ExtensionGeometryEntityReader: IExtension,
    IExtensionUtility
{
    /// <inheritdoc />
    public IExtensionInfo? Info { get; set; }
    
    /// <inheritdoc />
    public void Run(IExtensionUtilityContext context, out TResultStatus resultStatus)
    {
        try
        {
            resultStatus = default;
            
            // get all selected nodes
            using var applicationCom = ComWrapper.Create(context.CamApplication);
            using var activeProjectCom = applicationCom.InvokeAndWrap(application => 
                (application.GetActiveProject(out var status), status));
            using var geomModelCom = activeProjectCom.InvokeAndWrap(project => project.CAMAPIGeomModel);
            using var iteratorCom =
                geomModelCom.InvokeAndWrap(geomModel => (geomModel.GetNodes(out var status), status));
            var nodes = new List<ICAMAPIGeometryTreeNode>();
            foreach (var node in iteratorCom.AsEnumerable())
            {
                if (!node.Selected)
                    continue;
                nodes.Add(node);
            }

            // read selected nodes
            var message = "";
            foreach (var node in nodes)
            {
                using var nodeEntityCom = ComWrapper.Create(node.GeometryEntity);
                var localMessage = nodeEntityCom.Invoke(nodeEntity =>
                {
                    if (nodeEntity is ICamApiCSGeometryEntity entityCs)
                        return ReadEntityCs(entityCs);
                    if (nodeEntity is ICamApiCurveGeometryEntity entityCurve)
                        return ReadEntityCurve(entityCurve);
                    if (nodeEntity is ICamApiFaceGeometryEntity entityFace)
                        return ReadEntityFace(entityFace);
                    return "Unknown entity type";
                });
                message += $"{node.FullName} {localMessage}\n-----\n";
            }
            
            // show the message
            if (string.IsNullOrEmpty(message))
                message = "No selected items";
            ShowMessage("Selected items", message);
        }
        catch (Exception e)
        {
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = e.Message;
        }
    }
    
    private string ReadEntityCs(ICamApiCSGeometryEntity entityCs)
    {
        var matrix = entityCs.Matrix;
        return $"is coordinate system\n" +
               $"Center: {matrix.vT.X}, {matrix.vT.Y}, {matrix.vT.Z}";
    }
    
    private string ReadEntityCurve(ICamApiCurveGeometryEntity entityCurve)
    {
        using var curveCom = ComWrapper.Create(entityCurve.Curve);
        return curveCom.Invoke(curve => $"is curve\n" +
                                        $"Length: {curve.FullLen}");
    }
    
    private string ReadEntityFace(ICamApiFaceGeometryEntity entityFace)
    {
        using var faceCom = ComWrapper.Create(entityFace.Face);
        return faceCom.Invoke(face =>
        {
            if (face.IsCylindricalHole(0.01,
                    out var center,
                    out var axis,
                    out var radius,
                    out var zMin,
                    out var zMax))
            {
                return $"is cylindrical hole\n" +
                       $"Center: {center.X}, {center.Y}, {center.Z}\n" +
                       $"Axis: {axis.X}, {axis.Y}, {axis.Z}\n" +
                       $"Radius: {radius}\n" +
                       $"ZMin: {zMin}\n" +
                       $"ZMax: {zMax}";
            }
            return "is face";
        });
    }
    
    private void ShowMessage(string title, string message)
    {
        using var helperCom = UIDialogs.CreateHelper();
        var helper = helperCom.Instance
                     ?? throw new Exception("Failed to create UIDialogs helper");
        var buttons = MessageBoxHelper.BuildButtons(TUIButtonType.btOk);
        helper.MessageBox(message, TMessageDialogType.mdtInformation, buttons, TUIButtonType.btOk, title);
    }
}


