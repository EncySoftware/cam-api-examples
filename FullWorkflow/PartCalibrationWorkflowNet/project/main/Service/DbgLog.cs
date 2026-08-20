using System.Diagnostics;
using System.IO;

namespace PartCalibrationWorkflowNet.Service;

/// <summary>
/// Append-only debug log to %TEMP%\PartCalibrationWorkflow_dbg_&lt;pid&gt;.log.
/// Used only while diagnosing the host-side crash; intentionally cheap and
/// resilient to disk errors.
/// </summary>
internal static class DbgLog
{
    private static readonly string Path = System.IO.Path.Combine(
        System.IO.Path.GetTempPath(),
        $"PartCalibrationWorkflow_dbg_{Environment.ProcessId}.log");

    public static void Write(string msg)
    {
        try
        {
            File.AppendAllText(
                Path,
                $"{DateTime.Now:HH:mm:ss.fff} [{Environment.CurrentManagedThreadId}] {msg}{Environment.NewLine}");
        }
        catch
        {
            // Don't let logging itself crash the extension.
        }
    }

    public static void Write(string label, Exception ex)
    {
        Write($"{label}: {ex.GetType().Name}: {ex.Message}");
        Write(new StackTrace(ex, fNeedFileInfo: true).ToString());
    }
}
