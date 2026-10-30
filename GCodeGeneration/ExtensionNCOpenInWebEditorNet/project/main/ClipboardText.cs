using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace ExtensionNCOpenInWebEditorNet;

/// <summary>
/// Puts text on the Windows clipboard from any thread
/// </summary>
public static class ClipboardText
{
    /// <summary>
    /// Set the clipboard text on a dedicated STA thread and wait until it is done.
    /// An error on that thread is thrown again on the calling thread
    /// </summary>
    public static void Set(string text)
    {
        Exception? error = null;

        var thread = new Thread(() =>
        {
            try
            {
                SetOnCurrentThread(text);
            }
            catch (Exception e)
            {
                error = e;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (error != null)
            ExceptionDispatchInfo.Capture(error).Throw();
    }

    // Not inlined: System.Windows.Forms is loaded when this method is called, inside the try above
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void SetOnCurrentThread(string text)
    {
        // copy: true leaves the text on the clipboard after this thread ends
        Clipboard.SetDataObject(text, copy: true, retryTimes: 10, retryDelay: 100);
    }
}
