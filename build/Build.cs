using System;
using System.IO;
using MicroCom.CodeGenerator;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Serilog;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode
    public static int Main() => Execute<Build>(x => x.CreateNugetPackages);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = Configuration.Release;

    [Parameter]
    readonly AbsolutePath Output = RootDirectory / "artifacts" / "packages";

    [Parameter]
    readonly AbsolutePath ProjectFile = RootDirectory / "AvaloniaUI.WebView.Packages.slnf";

    string CiRunNumber => Environment.GetEnvironmentVariable("GITHUB_RUN_NUMBER");
    
    string RefName => Environment.GetEnvironmentVariable("GITHUB_REF_NAME");

    Target OutputParameters => _ => _
        .Executes(() =>
        {
            Log.Information($"Configuration: {Configuration}");
            Log.Information($"Output: {Output}");
            Log.Information($"ProjectFile: {ProjectFile}");
            Log.Information($"CiRunNumber: {CiRunNumber}");
            Log.Information($"CiRunNumber: {RefName}");
            Log.Information($"Version: {GetVersion()}");
        });

    Target Compile => _ => _
        .DependsOn(OutputParameters)
        .DependsOn(CompileNative)
        .Executes(() =>
        {
            DotNetBuild(c => c
                .SetConfiguration(Configuration)
                .AddProperty("PackageVersion", GetVersion())
                .SetVerbosity(DotNetVerbosity.minimal)
                .SetBinaryLog(Output / "build.binlog")
                .SetProjectFile(ProjectFile)
            );
        });

    Target CreateNugetPackages => _ => _
        .DependsOn(OutputParameters)
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetPack(c => c
                .SetConfiguration(Configuration)
                .AddProperty("PackageVersion", GetVersion())
                .SetOutputDirectory(Output)
                .SetProject(ProjectFile));
        });
    
    Target GenerateCppHeaders => _ => _.Executes(() =>
    {
        var file = MicroComCodeGenerator.Parse(
            File.ReadAllText(RootDirectory / "src" / "AvaloniaUI.WebView.Core" / "NativeMac" / "webview.mcidl"));
        File.WriteAllText(RootDirectory / "native" / "AvaloniaUI.WebView.Native" / "inc" / "webview-native.h",
            file.GenerateCppHeader());
    });

    Target CompileNative => _ => _
        .DependsOn(GenerateCppHeaders)
        .OnlyWhenStatic(() => IsOsx)
        .Executes(() =>
        {
            var project = $"{RootDirectory}/native/AvaloniaUI.WebView.Native/src/OSX/WebView.Native.OSX.xcodeproj/";
            var args =
                $"-project {project} -configuration {Configuration} CONFIGURATION_BUILD_DIR={RootDirectory}/Build/Products/Release";
            ProcessTasks.StartProcess("xcodebuild", args).AssertZeroExitCode();
        });

    string GetVersion()
    {
        if (Version.TryParse(RefName, out var version))
        {
            return RefName;
        }
        else
        {
            return "1.0.999-cibuild" + int.Parse(CiRunNumber).ToString("0000000") + "-alpha";
        }
    }
}
