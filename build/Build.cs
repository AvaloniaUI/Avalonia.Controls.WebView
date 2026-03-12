using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NuGet.Versioning;
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

    [NuGetPackage("dotnet-ilrepack", "ILRepackTool.dll", Framework = "net8.0")] readonly Tool IlRepackTool = null!;

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

    Target IlMerge => _ => _
        .DependsOn(Compile)
        .DependsOn(RunTests)
        .Executes(() =>
        {
            string[] projectsToProcess =
            [
                "Avalonia.Controls.WebView",
                "Avalonia.Xpf.Controls.WebView"
            ];

            foreach (var project in (RootDirectory / "src").GlobFiles("**/*.csproj")
                     .Where(p => projectsToProcess.Contains(p.NameWithoutExtension)))
            {
                List<string> dependencies = ["Avalonia.Controls.WebView.Core"];

                var tfms = (project.Parent / "bin" / Configuration).GetDirectories();

                NukeExtensions.IlMerge.Merge(IlRepackTool,
                    assemblyName: Path.GetFileNameWithoutExtension(project.Name)!,
                    targets: tfms.Select(tfm => new IlMerge.MergeTargetFramework(tfm, dependencies.ToArray(),
                        tfm.ToString().Contains("android") ? GetExtraDepLibs() : null)),
                    internalize: false,
                    renameInternalized: false,
                    publicApiList: null,
                    signKey: Statics.AvaloniaStrongNameKey);
            }
        });

    Target CreateNugetPackages => _ => _
        .DependsOn(OutputParameters)
        .DependsOn(RunTests)
        .DependsOn(Compile)
        .DependsOn(IlMerge)
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
            baseVersionNumber: new Version(11, 4, 999),
            isPackingToLocalCache: RunningTargets.Concat(ScheduledTargets)
                .Any(t => t.Name == nameof(CopyPackagesToNuGetCache)))
        .ToString();

    static IEnumerable<string> GetExtraDepLibs()
    {
        // See https://github.com/gluck/il-repack/issues/399
        var androidSdk = NuGetPackageResolver.GetGlobalInstalledPackage("Microsoft.Android.Ref.34",
            new VersionRange(new NuGetVersion(1, 0, 0)), null)?.Directory;
        if (androidSdk is null)
        {
            throw new DirectoryNotFoundException("Unable to find installed \"Microsoft.Android.Ref.34\" nuget package.");
        }

        var androidRefs = androidSdk / "ref" / "net8.0";
        yield return androidRefs;
    }
}
