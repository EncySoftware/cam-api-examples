using System;
using System.Diagnostics;
using System.IO;
using CAMAPI.Application;
using CAMAPI.DotnetHelper;
using CAMAPI.Singletons;

namespace ExtensionUtilityExportInformationNet
{
    /// <summary>
    /// Shows the export result: opens the web viewer located in the Viewer subfolder
    /// next to the extension dll, or opens the result folder if the viewer is missing.
    /// </summary>
    public static class ViewerLauncher
    {
        private const string ViewerFolderName = "Viewer";
        private const string ViewerExeName = "ProjectInfoViewer.exe";
        private const string LogFileName = "viewer-launch.log";

        /// <summary>
        /// Uid of the utility (matches the value in <see cref="CAMAPI.ExtensionFactory"/>).
        /// Used to find the dll path via UtilityManager in order to launch the viewer.
        /// </summary>
        private const string UtilityUid = "Extension.Utility.ExportInformation.Net";

        /// <summary>
        /// Opens <paramref name="jsonFullPath"/> in the viewer; on failure
        /// opens the <paramref name="outputRoot"/> folder with the export result.
        /// Path diagnostics are written to viewer-launch.log in <paramref name="outputRoot"/>.
        /// </summary>
        public static void Show(
            string jsonFullPath,
            string outputRoot,
            ComWrapper<ICamApiApplication> applicationCom)
        {
            var log = new List<string>();
            try
            {
                if (LaunchViewer(jsonFullPath, applicationCom, log))
                {
                    log.Add("RESULT: viewer launched.");
                }
                else
                {
                    log.Add("RESULT: viewer NOT launched -> opening folder: " + outputRoot);
                    OpenFolder(outputRoot, log);
                }
            }
            finally
            {
                WriteLog(outputRoot, log);
            }
        }

        /// <summary>
        /// Launches the viewer from the Viewer subfolder next to the extension dll. Returns true
        /// if the viewer started; false — if it is missing or the launch failed.
        /// </summary>
        private static bool LaunchViewer(
            string jsonFullPath,
            ComWrapper<ICamApiApplication> applicationCom,
            List<string> log)
        {
            try
            {
                var extensionDir = ResolveExtensionDir(applicationCom, log);
                log.Add("extensionDir = " + (extensionDir ?? "<null>"));
                if (string.IsNullOrEmpty(extensionDir))
                {
                    log.Add("STOP: extensionDir is empty.");
                    return false;
                }

                var viewerExe = Path.Combine(extensionDir, ViewerFolderName, ViewerExeName);
                log.Add("viewerExe = " + viewerExe);
                log.Add("File.Exists(viewerExe) = " + File.Exists(viewerExe));
                if (!File.Exists(viewerExe))
                {
                    log.Add("STOP: viewer exe not found.");
                    return false;
                }

                Process.Start(new ProcessStartInfo(viewerExe)
                {
                    ArgumentList = { jsonFullPath },
                    UseShellExecute = true,
                    WorkingDirectory = extensionDir,
                });
                log.Add("Process.Start OK, jsonArg = " + jsonFullPath);
                return true;
            }
            catch (Exception e)
            {
                log.Add("EXCEPTION in LaunchViewer: " + e);
                return false;
            }
        }

        /// <summary>
        /// Returns the extension installation folder via the path to its dll
        /// (UtilityManager → utility by Uid → ModulePath). null — if the path
        /// could not be obtained (the caller then goes to the fallback).
        /// </summary>
        private static string? ResolveExtensionDir(
            ComWrapper<ICamApiApplication> applicationCom,
            List<string> log)
        {
            try
            {
                using var utilityManagerCom = applicationCom.InvokeAndWrap(app => app.UtilityManager);
                log.Add("UtilityManager obtained, IsNull = " + utilityManagerCom.IsNull);

                using var utilsListCom = utilityManagerCom.InvokeAndWrap(manager => manager.GetListInfo(out _));
                log.Add("GetListInfo obtained, IsNull = " + utilsListCom.IsNull);

                using var utilButtonCom = utilsListCom.InvokeAndWrap(list => list.GetByUid(UtilityUid));
                log.Add("GetByUid('" + UtilityUid + "') IsNull = " + utilButtonCom.IsNull);
                if (utilButtonCom.IsNull)
                    return null;

                var modulePath = utilButtonCom.Invoke(button => button.ModulePath);
                log.Add("ModulePath (raw) = '" + (modulePath ?? "<null>") + "'");
                if (string.IsNullOrEmpty(modulePath))
                    return null;

                // ModulePath comes with host macros ($(PROGRAM_APPDATA)\...\dll);
                // unfold them into a real path via the ICamApiPaths singleton.
                using var pathsCom = SystemExtensionFactory.GetSingletonExtension<ICamApiPaths>(
                    "Extension.Global.Singletons.Paths");
                if (pathsCom == null)
                {
                    log.Add("STOP: Paths singleton not available.");
                    return null;
                }

                var unfoldedPath = pathsCom.Invoke(paths => paths.TryUnfoldPath(modulePath));
                log.Add("ModulePath (unfolded) = '" + (unfoldedPath ?? "<null>") + "'");
                if (string.IsNullOrEmpty(unfoldedPath))
                    return null;

                var dir = Path.GetDirectoryName(Path.GetFullPath(unfoldedPath));
                log.Add("resolved extension dir = " + (dir ?? "<null>"));
                return dir;
            }
            catch (Exception e)
            {
                log.Add("EXCEPTION in ResolveExtensionDir: " + e);
                return null;
            }
        }

        /// <summary>
        /// Opens the export result folder in the system file manager.
        /// A failure to open must not break the export — the json is already written successfully.
        /// </summary>
        private static void OpenFolder(string folderPath, List<string> log)
        {
            try
            {
                if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
                {
                    log.Add("OpenFolder: folder missing: " + folderPath);
                    return;
                }

                Process.Start(new ProcessStartInfo(folderPath) { UseShellExecute = true });
                log.Add("OpenFolder OK: " + folderPath);
            }
            catch (Exception e)
            {
                log.Add("EXCEPTION in OpenFolder: " + e);
            }
        }

        /// <summary>
        /// Writes diagnostics of the current run to viewer-launch.log in the result folder.
        /// The file is overwritten on every run. A log write failure is ignored.
        /// </summary>
        private static void WriteLog(string outputRoot, List<string> log)
        {
            try
            {
                if (string.IsNullOrEmpty(outputRoot))
                    return;

                Directory.CreateDirectory(outputRoot);
                var header = "ViewerLauncher diagnostics @ " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                var lines = new List<string> { header };
                lines.AddRange(log);
                File.WriteAllLines(Path.Combine(outputRoot, LogFileName), lines);
            }
            catch (Exception)
            {
            }
        }
    }
}
