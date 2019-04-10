using System;
using System.IO;
using System.Threading.Tasks;
using static Bullseye.Targets;
using static Build.Buildary.Directory;
using static Build.Buildary.Path;
using static Build.Buildary.Shell;
using static Build.Buildary.Runner;
using static Build.Buildary.Log;
using static Build.Buildary.File;

namespace Build
{
    static class Program
    {
        static Task Main(string[] args)
        {
            var options = ParseOptions<Options>(args);
            var sha = ReadShell("git rev-parse --short HEAD").TrimEnd(Environment.NewLine.ToCharArray());
            
            Info($"Sha: {sha}");
            
            IPlatform platform = new Linux64Platform();
            
            Target("download", () =>
            {
                var downloadsDirectory = ExpandPath("./downloads");
                if (!DirectoryExists(downloadsDirectory))
                {
                    Directory.CreateDirectory(downloadsDirectory);
                }
                foreach (var remoteFileUrl in platform.GetUrls())
                {
                    var fileName = Path.GetFileName(remoteFileUrl);
                    var downloadedFilePath = Path.Combine(downloadsDirectory, fileName);
                    if (FileExists(downloadedFilePath))
                    {
                        continue;
                    }
                    Info($"Downloading: {remoteFileUrl}");
                    RunShell($"curl -Lo \"{downloadedFilePath}\" \"{remoteFileUrl}\"");
                }
            });
            
            Target("extract", () =>
            {
                var extractedDirectory = ExpandPath("./extracted");
                if (!DirectoryExists(extractedDirectory))
                {
                    Directory.CreateDirectory(extractedDirectory);
                }
                foreach (var remoteFileUrl in platform.GetUrls())
                {
                    var downloadedFilePath = Path.Combine(ExpandPath("./downloads"), Path.GetFileName(remoteFileUrl));
                    Info($"Extracting: {downloadedFilePath}");
                    RunShell($"cd \"{extractedDirectory}\" && 7za x {downloadedFilePath} -aoa");
                }
            });
            
            Target("package", () =>
            {
                // Create the output directory.
                var outputDirectory = ExpandPath("./output");
                if (!DirectoryExists(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }

                if (DirectoryExists(ExpandPath("./tmp")))
                {
                    DeleteDirectory(ExpandPath("./tmp"));
                }
                RunShell("cp -r ./extracted ./tmp");
                
                platform.PackageDev(ExpandPath("./extracted"), ExpandPath($"./output/qt-{platform.QtVersion}-{platform.PlatformArch}-dev-{sha}.tar.gz"));
                
                if (DirectoryExists(ExpandPath("./tmp")))
                {
                    DeleteDirectory(ExpandPath("./tmp"));
                }
                RunShell("cp -r ./extracted ./tmp");
                
                platform.PackageRuntime(ExpandPath("./extracted"), ExpandPath($"./output/qt-{platform.QtVersion}-{platform.PlatformArch}-runtime-{sha}.tar.gz"));
            });

            Target("default", DependsOn("download", "extract", "package"));
            
            return Run(options);
        }

        // ReSharper disable ClassNeverInstantiated.Local
        class Options : RunnerOptions
        // ReSharper restore ClassNeverInstantiated.Local
        {
        }
    }
}
