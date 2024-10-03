using System.Runtime.InteropServices;

namespace ExtensionUtilityProjectToolsListNet;

/// <summary>
/// Wrapper over COM object, provided by CAMAPI. It releases object after using
/// </summary>
/// <typeparam name="T">Any interface from CAMAPI</typeparam>
public class ApiComObject<T> : IDisposable
{
    /// <summary>
    /// Wrapper over COM object, provided by CAMAPI. It releases object after using
    /// </summary>
    /// <param name="obj">Object created in main CAM application</param>
    public ApiComObject(T? obj)
    {
        if (obj == null)
            throw new ArgumentNullException(nameof(obj));
        Instance = obj;
    }

    /// <summary>
    /// Wrapping object
    /// </summary>
    public T Instance { get; }

    /// <summary>
    /// Release COM object
    /// </summary>
    public void Dispose()
    {
        if (Instance != null)
            Marshal.ReleaseComObject(Instance);
    }
}