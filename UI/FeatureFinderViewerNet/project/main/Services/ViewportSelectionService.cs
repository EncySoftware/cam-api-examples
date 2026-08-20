using CAMAPI.Application;
using CAMAPI.DotnetHelper;

namespace FeatureFinderViewerNet;

class ViewportSelectionService : IViewportSelectionService
{
    private readonly ComWrapper<ICamApiApplication> _appCom;

    public ViewportSelectionService(ComWrapper<ICamApiApplication> appCom)
    {
        _appCom = appCom;
    }

    public void Select(IEnumerable<string> entityPaths)
    {
        try
        {
            using var projectCom = _appCom.GetActiveProject();
            if (projectCom.IsNull)
                return;
            using var geomModelCom = projectCom.CAMAPIGeomModel();
            geomModelCom.DeselectAll();
            foreach (var path in entityPaths)
            {
                using var nodeCom = geomModelCom.FindByFullName(path);
                if (!nodeCom.IsNull)
                    nodeCom.SetSelected(true);
            }
        }
        catch
        {
            // best-effort — node may no longer exist after project reload
        }
    }
}
