using CAMAPI.Extensions;
using CAMAPI.Application;
using CAMAPI.DotnetHelper;
using CAMAPI.GeomModel;
using CAMAPI.ResultStatus;
using System.Runtime.InteropServices;

namespace ExtensionUtilityGeometryModelNet;

public class GeometryModelExportExample : IExtension, IExtensionUtility
{
    public IExtensionInfo? Info { get; set; }

    private const string fileName = "GeometryModelExportExample.osd";

    public void Run(IExtensionUtilityContext Context, out TResultStatus result)
    {        
        result = default;       
        var prj = Context.CamApplication.GetActiveProject(out TResultStatus r);
        try
        {
            if (r.Code == TResultStatusCode.rsSuccess)
            {
                var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Exported Example Models", fileName);
                using var fullModel = new ApiComObject<ICAMAPIGeometryModel>(prj.CAMAPIGeomModel);
                fullModel.Instance.ExportSelectedToOSD(filePath, out result);

                if (!(result.Code == TResultStatusCode.rsSuccess))
                    throw new Exception(result.Description);
            }
        } finally {
            Marshal.ReleaseComObject(prj);
        }
    }
}
