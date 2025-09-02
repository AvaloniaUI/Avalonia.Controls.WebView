using System;
using System.Collections.Generic;
using System.Linq;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using NukeExtensions;
using Serilog;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

class Build : NukeBuild
{
    public static int Main() => IsLocalBuild ?
        Execute<Build>(x => x.CopyPackagesToNuGetCache) :
        Execute<Build>(x => x.CreateNugetPackages);

    [NuGetPackage("Babel.Obfuscator.Tool", "babel.dll", Framework = "net9.0")] readonly Tool Babel = null!;

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = Configuration.Release;
    [Parameter]
    readonly AbsolutePath Output = RootDirectory / "artifacts" / "packages";
    [Parameter]
    readonly bool? Obfuscate;

    readonly AbsolutePath SolutionFile = RootDirectory / "Avalonia.Controls.WebView.ci.slnf";

    Target OutputParameters => _ => _
        .Executes(() =>
        {
            Log.Information("Configuration: {Configuration}", Configuration);
            Log.Information("Output: {AbsolutePath}", Output);
            Log.Information("Version: {GetVersion}", GetVersion());
        });

    Target Compile => _ => _
        .DependsOn(OutputParameters)
        .DependsOn(RunTests)
        .Executes(() => DotNetBuild(c => c
            .SetProjectFile(SolutionFile)
            .SetVersion(GetVersion())
            .AddProperty("ILMergeBuild", true)
            .SetConfiguration(Configuration)
        ));

    Target RunTests => _ => _
        .DependsOn(OutputParameters)
        .Executes(() => DotNetTest(c => c
            .SetProjectFile(SolutionFile)
            .SetVerbosity(DotNetVerbosity.minimal)
            .SetConfiguration(Configuration)
        ));

    Target RunObfuscate => _ => _
        .OnlyWhenStatic(ShouldObfuscate)
        .DependsOn(Compile)
        .Executes(() =>
        {
            string[] projectsToObfuscate =
            [
                "Avalonia.Controls.WebView",
                "Avalonia.Xpf.Controls.WebView"
            ];
            foreach (var project in (RootDirectory / "src").GlobFiles("**/*.csproj")
                     .Where(p => projectsToObfuscate.Contains(p.NameWithoutExtension)))
            {
                List<string> dependencies = ["Avalonia.Controls.WebView.Core"];
                if (project.NameWithoutExtension != "Avalonia.Xpf.Controls.WebView")
                {
                    dependencies.Add("AvaloniaUI.Licensing");
                }

                var tfms = (project.Parent / "bin" / Configuration).GetDirectories();
                NukeExtensions.Babel.Obfuscate(
                    Babel,
                    assemblyName: project.NameWithoutExtension,
                    targets: tfms.Select(tfm => new Babel.ObfuscationTargetFramework(tfm, dependencies.ToArray())),
                    signKey: Statics.AvaloniaStrongNameKey,
                    licenseFile: Statics.BabelLicense,
                    rulesFiles: [
                        Statics.BabelRules,
                        RootDirectory / "build" / "BabelWebView.rules"
                    ]);
            }
        });

    Target CreateNugetPackages => _ => _
        .DependsOn(OutputParameters)
        .DependsOn(RunTests)
        .DependsOn(Compile)
        .DependsOn(RunObfuscate)
        .Executes(() =>
        {
            var srcRootDirectory = RootDirectory / "src";
            foreach (var srcProject in srcRootDirectory.GlobFiles("**/*.csproj"))
            {
                DotNetPack(c => c
                    .SetProject(srcProject)
                    .SetNoBuild(true)
                    .SetNoRestore(true)
                    .SetContinuousIntegrationBuild(true)
                    .AddProperty("PackageVersion", GetVersion())
                    .SetConfiguration(Configuration)
                    .SetOutputDirectory(Output)
                );
            }
        });

    Target CopyPackagesToNuGetCache => _ => _
        .DependsOn(CreateNugetPackages)
        .Executes(() => NugetCache.InstallLibraryToNuGetCache(
            Output.GlobFiles("*.nupkg"),
            RootDirectory,
            GetVersion()));

    string GetVersion() => VersionResolver
        .GetGitHubVersion(
            baseVersionNumber: new Version(11, 3, 999),
            isPackingToLocalCache: RunningTargets.Concat(ScheduledTargets)
                .Any(t => t.Name == nameof(CopyPackagesToNuGetCache)))
        .ToString();
    bool ShouldObfuscate() => Obfuscate ?? (Configuration == Configuration.Release);
}
