using System.Runtime.InteropServices;

namespace ExtensionNetProject;

/// <summary>
/// Wrapper over COM interop for marshalling objects. It is used to give access to COM objects from any thread
/// </summary>
public class ComReleaser: IDisposable
{
    /// <summary>
    /// Pointer to COM-object
    /// </summary>
    private object[] Objects { get; }
    
    /// <summary>
    /// Flag: is object disposed
    /// </summary>
    private bool _isDisposed;

    /// <summary>
    /// Create wrapper for COM object
    /// </summary>
    public ComReleaser(params object[] comObjects)
    {
        Objects = comObjects;
    }
    
    /// <summary>
    /// Clean up resources
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
            return;
        
        _isDisposed = true;
        foreach (var comObject in Objects)
        {
            // tell GC to not release object, because we will do it manually
            GC.SuppressFinalize(comObject);
            
            // do it manually
            Marshal.ReleaseComObject(comObject);
        }
    }
}