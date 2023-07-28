using System;
using System.IO;
using System.Linq;
using MicroCom.CodeGenerator;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;

class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main () => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
        });

    Target Restore => _ => _
        .Executes(() =>
        {
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .DependsOn(CompileNative)
        .Executes(() =>
        {
        });

    Target GenerateCppHeaders => _ => _.Executes(() =>
    {
        var file = MicroComCodeGenerator.Parse(
            File.ReadAllText(RootDirectory / "src" / "AvaloniaWebView" / "Native" / "webview.idl"));
        File.WriteAllText(RootDirectory / "native" / "AvaloniaWebView.Native" / "inc" / "webview-native.h",
            file.GenerateCppHeader());
    });
    
    Target CompileNative => _ => _
        .DependsOn(Clean)
        .DependsOn(GenerateCppHeaders)
        .OnlyWhenStatic(() => EnvironmentInfo.IsOsx)
        .Executes(() =>
        {
            var project = $"{RootDirectory}/native/AvaloniaWebView.Native/src/OSX/WebView.Native.OSX.xcodeproj/";
            var args = $"-project {project} -configuration {Configuration} CONFIGURATION_BUILD_DIR={RootDirectory}/Build/Products/Release";
            ProcessTasks.StartProcess("xcodebuild", args).AssertZeroExitCode();
        });
}
