using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using MicroCom.CodeGenerator;
using NuGet.Configuration;
using NuGet.Versioning;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.PowerShell;
using Serilog;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode
    public static int Main() => IsLocalBuild ?
        Execute<Build>(x => x.CopyPackagesToNuGetCache) :
        Execute<Build>(x => x.CreateNugetPackages);

    [NuGetPackage("dotnet-ilrepack", "ILRepackTool.dll", Framework = "net8.0")] readonly Tool IlRepackTool;

    [PathVariable] readonly Tool Babel;

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = Configuration.Release;

    [Parameter]
    readonly AbsolutePath Output = RootDirectory / "artifacts" / "packages";

    string CiRunNumber => Environment.GetEnvironmentVariable("GITHUB_RUN_NUMBER");
    
    string RefName => Environment.GetEnvironmentVariable("GITHUB_REF_NAME");

    Target OutputParameters => _ => _
        .Executes(() =>
        {
            Log.Information($"Configuration: {Configuration}");
            Log.Information($"Output: {Output}");
            Log.Information($"CiRunNumber: {CiRunNumber}");
            Log.Information($"CiRunNumber: {RefName}");
            Log.Information($"Version: {GetVersion()}");
        });

    Target Compile => _ => _
        .DependsOn(OutputParameters)
        .Executes(() =>
        {
            foreach (var srcProject in (RootDirectory / "src").GlobFiles("**/*.csproj"))
            {
                DotNetBuild(c => c
                    .SetProjectFile(srcProject)
                    .SetVerbosity(DotNetVerbosity.minimal)
                    .AddProperty("PackageVersion", GetVersion())
                    .SetVersion(GetVersion())
                    .SetConfiguration(Configuration)
                );
            }
        });

    Target IlMerge => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            var mergeRootProjects = (RootDirectory / "src").GlobFiles("**/*.csproj").Where(p =>
                p.Name.Contains("Avalonia.Controls.WebView.csproj") ||
                p.Name.Contains("Avalonia.Xpf.Controls.WebView"));

            var libs = string.Join(' ', GetExtraDepLibs().Select(l => $"/lib:{l}"));
            var coreProjectPublicApi = (RootDirectory / "src").GlobFiles("**/Avalonia.Controls.WebView.Core.csproj")
                .First().Parent / "public-api.txt";

            foreach (var mergeRootProject in mergeRootProjects)
            {
                var projectName = Path.GetFileNameWithoutExtension(mergeRootProject);
                var mergeRootDlls = mergeRootProject.Parent
                    .GlobFiles(Path.Combine("bin", Configuration, "**", projectName + ".dll"));
                foreach (var mergeRootDll in mergeRootDlls)
                {
                    string[] depNamesToMerge = ["Avalonia.Controls.WebView.Core.dll"];
                    var dependenciesToMerge = mergeRootDll.Parent
                        .GlobFiles("*.dll")
                        .Where(f => Array.IndexOf(depNamesToMerge, f.Name) >= 0);

                    var dependenciesArg = string.Join(" ", dependenciesToMerge.Select(dll => '"' + dll + '"'));
                    var signParams = "";// $"/keyfile:{RootDirectory / "build" / "avalonia.snk"}";

                    IlRepackTool.Invoke(
                        $"""/internalize:{coreProjectPublicApi} /renameinternalized /parallel /ndebug {libs:nq} {signParams} /out:"{mergeRootDll}" "{mergeRootDll}" {dependenciesArg} """,
                        mergeRootDll.Parent);
                }
            }
        });

    Target Obfuscate => _ => _
        .DependsOn(Compile)
        .DependsOn(IlMerge)
        .Executes(() =>
        {
            if (!OperatingSystem.IsWindows())
            {
                Log.Warning("Obfuscation is skipped");
                if (!IsLocalBuild)
                {
                    throw new InvalidOperationException("Babel requires Windows CI machine");
                }

                return;
            }

            string[] projectsToObfuscate =
            [
                "Avalonia.Controls.WebView",
                "Avalonia.Xpf.Controls.WebView"
            ];
            foreach (var projectName in projectsToObfuscate)
            {
                Log.Information("Obfuscating {Project}", projectName);

                var projectRoot = RootDirectory / "src" / projectName;
                var obfuscationMapFile = RootDirectory / "Obfuscated" / (projectName + ".ObfuscationMap.xml");
                var obfuscationLogFile = RootDirectory / "Obfuscated" / (projectName + ".Obfuscation.log");
                var rules = RootDirectory / "build" / "Babel.rules";

                foreach (var buildOutput in (projectRoot / "bin" / Configuration).GlobDirectories("net*"))
                {
                    var dllFile = buildOutput / (projectName + ".dll");
                    
                    Babel($"{dllFile} --nologo --rules {rules} --output {dllFile}  --mapout {obfuscationMapFile} --logfile {obfuscationLogFile}", RootDirectory);
                }
            }
        });

    Target CreateNugetPackages => _ => _
        .DependsOn(OutputParameters)
        .DependsOn(Compile)
        .DependsOn(IlMerge)
        .DependsOn(Obfuscate)
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
        .Executes(() =>
        {
            var globalPackagesFolder = SettingsUtility.GetGlobalPackagesFolder(
                Settings.LoadDefaultSettings(RootDirectory));

            var packageFiles = Output.GlobFiles("*.nupkg");
            if (packageFiles.Count == 0)
            {
                throw new InvalidOperationException("No nupkg files were found.");
            }

            foreach (var path in packageFiles)
            {
                using var f = File.Open(path.ToString(), FileMode.Open, FileAccess.Read);
                using var zip = new ZipArchive(f, ZipArchiveMode.Read);
                var nuspecEntry = zip.Entries.First(e => e.FullName.EndsWith(".nuspec") && e.FullName == e.Name);
                var packageId = XDocument.Load(nuspecEntry.Open()).Document!.Root!
                    .Elements().First(x => x.Name.LocalName == "metadata")
                    .Elements().First(x => x.Name.LocalName == "id").Value;

                var packagePath = Path.Combine(
                    globalPackagesFolder,
                    packageId.ToLowerInvariant(),
                    GetVersion());

                if (Directory.Exists(packagePath))
                    Directory.Delete(packagePath, true);
                Directory.CreateDirectory(packagePath);
                zip.ExtractToDirectory(packagePath);
                File.WriteAllText(Path.Combine(packagePath, ".nupkg.metadata"), @"{
  ""version"": 2,
  ""contentHash"": ""FnIKqnvWIoQ+6ZZcVGX0dZyFA9A5GaRFTfTK+bj3coj0Eb528+4GADTMTIb2pmx/lpi79ZXJAln1A+Lyr+i6Vw=="",
  ""source"": ""https://api.nuget.org/v3/index.json""
}");
                Log.Information("Package path is " + packagePath);
            }
        });

    string GetVersion()
    {
        if (RunningTargets.Concat(ScheduledTargets).Any(t => t.Name == nameof(CopyPackagesToNuGetCache)))
        {
            return "9999.0.0-localbuild";
        }
        if (Version.TryParse(RefName, out var version))
        {
            return RefName;
        }
        else if (Regex.Match(RefName ?? "", """release\/(?<ver>[\d\.]*)""") is { Success: true } match)
        {
            return match.Groups["ver"].Value;
        }
        else if (CiRunNumber is not null)
        {
            return "1.0.999-cibuild" + int.Parse(CiRunNumber).ToString("0000000") + "-alpha";
        }

        return "1.0.999-localbuild-alpha";
    }

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
