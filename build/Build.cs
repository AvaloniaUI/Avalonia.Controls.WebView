using System.IO;
using MicroCom.CodeGenerator;
using Nuke.Common;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
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

    Target Compile => _ => _
        .DependsOn(CompileNative)
        .Executes(() =>
        {
            DotNetBuild(c => c
                .SetConfiguration(Configuration)
                .SetProjectFile(RootDirectory / "AvaloniaUI.WebView.sln")
            );
        });

    Target CreateNugetPackages => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetPack(c => c
                .SetConfiguration(Configuration)
                .SetProject(RootDirectory / "AvaloniaUI.WebView.sln"));
        });
    
    Target GenerateCppHeaders => _ => _.Executes(() =>
    {
        var file = MicroComCodeGenerator.Parse(
            File.ReadAllText(RootDirectory / "src" / "AvaloniaUI.WebView.Core" / "Native" / "webview.idl"));
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
}
