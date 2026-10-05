using System.Globalization;
using System.Runtime.InteropServices;
using DemoMath; // Generated .NET projection of DemoMath.idl.

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

Marshal.ThrowExceptionForHR(Native.GetGeomLibrary(out var pointer));
if (pointer == IntPtr.Zero)
    throw new COMException("GetGeomLibrary returned a null pointer");

IGeomLibrary? library = null;
IGeomCalculator? calculator = null;
try
{
    library = (IGeomLibrary)Marshal.GetObjectForIUnknown(pointer);
    Console.WriteLine($"Library: {library.Name}");

    calculator = library.CreateGeomCalculator("Robot path");
    calculator.Name = "Robot path geometry";
    Console.WriteLine($"Calculator: {calculator.Name}");

    var point = new TPoint3D { X = 1, Y = 2, Z = 3 };
    var offset = new TVector3D { X = 4, Y = -1, Z = 2 };
    var shifted = calculator.Translate(point, offset);
    var distance = calculator.Distance(point, shifted);
    Console.WriteLine($"Translated: {calculator.DescribePoint(shifted)}");
    Console.WriteLine($"Distance: {distance.ToString("F3", CultureInfo.InvariantCulture)}");

    calculator.Normalize(new TVector3D { X = 3, Y = 4, Z = 0 },
        out var unitVector, out var length);
    Console.WriteLine($"Normalized: ({unitVector.X:F3}, {unitVector.Y:F3}, {unitVector.Z:F3}); length {length:F3}");

    // Matrix columns are basis vectors; Origin is the translation.
    var matrix = new TMatrix3D
    {
        AxisX = new TVector3D { X = 0, Y = 1, Z = 0 },
        AxisY = new TVector3D { X = -1, Y = 0, Z = 0 },
        AxisZ = new TVector3D { X = 0, Y = 0, Z = 1 },
        Origin = new TPoint3D { X = 10, Y = 20, Z = 30 }
    };
    var transformed = calculator.Transform(point, matrix);
    Console.WriteLine($"Transformed: {calculator.DescribePoint(transformed)}");
}
finally
{
    if (calculator is not null)
        Marshal.FinalReleaseComObject(calculator);
    if (library is not null)
        Marshal.FinalReleaseComObject(library);
    Marshal.Release(pointer); // Release the factory's owned reference.
}

internal static class Native
{
    [DllImport("NativeMath.dll", EntryPoint = "GetGeomLibrary",
        ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    internal static extern int GetGeomLibrary(out IntPtr library);
}
