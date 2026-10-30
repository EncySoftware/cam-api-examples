using AppStarterInterface;
using AssemblyLoaderTypes;
using CAMHelper.STAssemblyWrapper;

namespace ApplicationSimpleDemoNet;

/// <summary>
/// Lets the native code load .NET assemblies through <see cref="STAssemblyLoader"/>.
/// Passed to SCStarterNativeUtils.dll via <see cref="StarterNativeUtils.SetAppStarter"/>.
/// </summary>
public class SCAppStarterShell : ICAMAppStarter
{
    /// <summary>
    /// Creates an Assembly/AssemblyLoadContext wrapper.
    /// </summary>
    public IAssemblyLoader CreateAssemblyLoader() => new STAssemblyLoader();
}
