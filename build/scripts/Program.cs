using System;
using System.Collections.Generic;
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

            var platforms = new Dictionary<string, IPlatform>
            {
                { "linux-x64", new Linux64Platform() },
                { "osx-x64", new OSX64Platform() },
                { "win-x64", new Windows64Platform() }
            };

            if (string.IsNullOrEmpty(options.Platform))
            {
                Failure("You must provide a platform.");
                Environment.Exit(1);
            }
            
            if (!platforms.ContainsKey(options.Platform))
            {
                Failure($"No platform exists for {options.Platform}");
                Environment.Exit(1);
            }

            var platform = platforms[options.Platform];
            
            
            var fullVersion = $"qt-{platform.QtVersion}-{sha}-{platform.PlatformArch}";
            Info($"Full version: {fullVersion}");
            
            Target("clean", () =>
            {
                if(DirectoryExists(ExpandPath("./tmp")))
                    DeleteDirectory(ExpandPath("./tmp"));
                if(DirectoryExists(ExpandPath("./extracted")))
                    DeleteDirectory(ExpandPath("./extracted"));
            });

            string ConvertRemoteUrlToLocalFilePath(string url)
            {
                return url.Replace(":", "").Replace("/", "");
            }
            
            Target("download", () =>
            {
                var downloadsDirectory = ExpandPath("./downloads");
                if (!DirectoryExists(downloadsDirectory))
                {
                    Directory.CreateDirectory(downloadsDirectory);
                }
                foreach (var remoteFileUrl in platform.GetUrls())
                {
                    var downloadedFilePath = Path.Combine(downloadsDirectory, ConvertRemoteUrlToLocalFilePath(remoteFileUrl));
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
                if (DirectoryExists(extractedDirectory))
                {
                    DeleteDirectory(extractedDirectory);
                }
                Directory.CreateDirectory(extractedDirectory);
                
                foreach (var remoteFileUrl in platform.GetUrls())
                {
                    var downloadedFilePath = Path.Combine(ExpandPath("./downloads"), ConvertRemoteUrlToLocalFilePath(remoteFileUrl));
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
                
                platform.PackageDev(ExpandPath("./tmp"), ExpandPath($"./output/{fullVersion}-dev.tar.gz"), fullVersion);
                
                if (DirectoryExists(ExpandPath("./tmp")))
                {
                    DeleteDirectory(ExpandPath("./tmp"));
                }
                RunShell("cp -r ./extracted ./tmp");
                
                platform.PackageRuntime(ExpandPath("./tmp"), ExpandPath($"./output/{fullVersion}-runtime.tar.gz"), fullVersion);
            });

            Target("default", DependsOn("clean", "download", "extract", "package"));
            
            return Run(options);
        }

        // ReSharper disable ClassNeverInstantiated.Local
        class Options : RunnerOptions
        // ReSharper restore ClassNeverInstantiated.Local
        {
            [PowerArgs.ArgDefaultValue("linux-64")]
            public string Platform { get; set; }
        }
    }
}
