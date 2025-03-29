using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using MicroCom.CodeGenerator;
using NuGet.Configuration;
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

    [NuGetPackage("dotnet-ilrepack", "ILRepackTool.dll", Framework = "net8.0")] readonly Tool IlRepackTool;

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
            var srcRootDirectory = RootDirectory / "src";
            foreach (var srcProject in srcRootDirectory.GlobFiles("**/*.csproj"))
            {
                if (srcProject.Name.Contains("Xpf") && CiRunNumber is not null)
                {
                    // Skip XPF on CI
                    continue;
                }

                DotNetBuild(c => c
                    .SetProjectFile(srcProject)
                    .SetVerbosity(DotNetVerbosity.minimal)
                    .AddProperty("PackageVersion", GetVersion())
                    .SetVersion(GetVersion())
                    .SetConfiguration(Configuration)
                );
            }
        });

    Target CreateNugetPackages => _ => _
        .DependsOn(OutputParameters)
        .DependsOn(Compile)
        .Executes(() =>
        {
            var srcRootDirectory = RootDirectory / "src";
            foreach (var srcProject in srcRootDirectory.GlobFiles("**/*.csproj"))
            {
                if (srcProject.Name.Contains("Xpf") && CiRunNumber is not null)
                {
                    // Skip XPF on CI
                    continue;
                }

                DotNetPack(c => c
                    .SetProject(srcProject)
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
        if (ScheduledTargets.Any(t => t.Name == nameof(CopyDiagnosticsToNuGetCache)))
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
}
