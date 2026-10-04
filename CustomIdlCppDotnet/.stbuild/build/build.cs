using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using Nuke.Common;

public class DemoBuild : NukeBuild
{
    public static int Main() => Execute<DemoBuild>(x => x.Run);

    [Parameter("Build variant")]
    public readonly string Variant = "Release";

    private string ExampleRoot => Path.GetFullPath(Path.Combine(RootDirectory, ".."));

    // The Delphi builder is intentionally disabled: the example exchanges only
    // a C++ header and a .NET interop assembly, so no Delphi compiler is required.
    // new BuilderMsDelphiProps { Name = "BuilderDelphi" },

    private Target Compile => _ => _.Executes(() =>
    {
        var projects = ReadProjects();
        CompileIdl(projects["Idl"]);
        CompileCpp(projects["Cpp"]);
        CompileCSharp(projects["CSharp"]);
    });

    private Target Run => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            var client = Path.Combine(ExampleRoot, "Client", "bin", Variant,
                "net10.0-windows", "Client.exe");
            RunTool(client, ExampleRoot);
        });

    private Dictionary<string, string> ReadProjects()
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        using var manifest = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(RootDirectory, "buildspace.json")));
        foreach (var item in manifest.RootElement.GetProperty("projects").EnumerateArray())
        {
            var settingsPath = Path.GetFullPath(Path.Combine(RootDirectory, item.GetString()!));
            using var project = JsonDocument.Parse(File.ReadAllText(settingsPath));
            var type = project.RootElement.GetProperty("type").GetString()!;
            var source = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(settingsPath)!,
                project.RootElement.GetProperty("project_path").GetString()!));
            if (!result.TryAdd(type, source))
                throw new InvalidOperationException($"Duplicate project type: {type}");
        }
        foreach (var type in new[] { "Idl", "Cpp", "CSharp" })
            if (!result.ContainsKey(type))
                throw new InvalidOperationException($"Missing {type} project in buildspace.json");
        return result;
    }

    private void CompileIdl(string source)
    {
        var generated = Path.Combine(ExampleRoot, "Generated");
        var tlbFolder = Path.Combine(generated, "tlb");
        Directory.CreateDirectory(generated);
        Directory.CreateDirectory(tlbFolder);

        // MIDL and TlbImp are bundled with STBuild. We invoke only the two needed
        // projections; the standard IDL pipeline also creates Delphi PAS/BPL files.
        var tools = AppContext.BaseDirectory;
        var midl = Path.Combine(tools, "idl2tlb", "Midl.Exe");
        var cpp = Path.Combine(tools, "idl2tlb", "CL", "cl.exe");
        var includes = Path.Combine(tools, "idl2tlb", "include");
        var tlb = Path.Combine(tlbFolder, "DemoMath.tlb");
        RunTool(midl, generated, source, "/ms_ext", "/cpp_cmd", cpp,
            "/tlb", tlb, "/I" + includes, "/I" + tlbFolder);

        var tlbImp = Path.Combine(tools, "tlb_gen_dotnet_dll", "v10.0A", "NETFX4.8", "TlbImp.exe");
        var keyFile = Path.Combine(tools, "tlb_gen_dotnet_dll", "STKey.snk");
        RunTool(tlbImp, generated, tlb, "/primary", "/keyfile:" + keyFile,
            "/namespace:DemoMath", "/out:" + Path.Combine(generated, "DemoMath.dll"));
    }

    private void CompileCpp(string project)
    {
        var msBuild = FindMsBuild();
        RunTool(msBuild, ExampleRoot, project, "/p:Configuration=" + Variant,
            "/p:Platform=x64", "/verbosity:minimal");
    }

    private void CompileCSharp(string project)
    {
        RunTool("dotnet", ExampleRoot, "build", project, "-c", Variant,
            "-p:NuGetAudit=false", "--nologo", "--ignore-failed-sources");
    }

    private static string FindMsBuild()
    {
        var overridePath = Environment.GetEnvironmentVariable("MSBUILD_EXE_PATH");
        if (!string.IsNullOrWhiteSpace(overridePath) && File.Exists(overridePath))
            return overridePath;
        var vsWhere = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            "Microsoft Visual Studio", "Installer", "vswhere.exe");
        if (File.Exists(vsWhere))
        {
            var output = RunTool(vsWhere, Directory.GetCurrentDirectory(),
                "-latest", "-products", "*", "-requires", "Microsoft.Component.MSBuild",
                "-find", @"MSBuild\**\Bin\MSBuild.exe");
            var path = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault(File.Exists);
            if (path is not null)
                return path;
        }
        throw new FileNotFoundException("MSBuild.exe was not found; set MSBUILD_EXE_PATH");
    }

    private static string RunTool(string fileName, string workingDirectory, params string[] arguments)
    {
        var startInfo = new ProcessStartInfo(fileName)
        {
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (var argument in arguments)
            startInfo.ArgumentList.Add(argument);
        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Could not start {fileName}");
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"{Path.GetFileName(fileName)} failed:\n{output}\n{error}");
        if (!string.IsNullOrWhiteSpace(output))
            Console.WriteLine(output.TrimEnd());
        return output;
    }
}
