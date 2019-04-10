using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using static Bullseye.Targets;
using static Build.Buildary.Directory;
using static Build.Buildary.Path;
using static Build.Buildary.Shell;
using static Build.Buildary.Runner;
using static Build.Buildary.Runtime;
using static Build.Buildary.Log;
using static Build.Buildary.File;
using static Build.Buildary.GitVersion;

namespace Build
{
    static class Program
    {
        static List<string> GetLinuxUrls()
        {
            var baseUrl = "https://download.qt.io/online/qtsdkrepository/linux_x64/desktop/qt5_5122/qt.qt5.5122.gcc_64/";

            return new List<string>
            {
                "5.12.2-0-201903121858icu-linux-Rhel7.2-x64.7z",
                "5.12.2-0-201903121858qt3d-Linux-RHEL_7_4-GCC-Linux-RHEL_7_4-X86_64.7z",
                "5.12.2-0-201903121858qtbase-Linux-RHEL_7_4-GCC-Linux-RHEL_7_4-X86_64.7z",
                "5.12.2-0-201903121858qtcanvas3d-Linux-RHEL_7_4-GCC-Linux-RHEL_7_4-X86_64.7z",
                "5.12.2-0-201903121858qtconnectivity-Linux-RHEL_7_4-GCC-Linux-RHEL_7_4-X86_64.7z",
                "5.12.2-0-201903121858qtdeclarative-Linux-RHEL_7_4-GCC-Linux-RHEL_7_4-X86_64.7z",
                "5.12.2-0-201903121858qtgamepad-Linux-RHEL_7_4-GCC-Linux-RHEL_7_4-X86_64.7z",
                "5.12.2-0-201903121858qtgraphicaleffects-Linux-RHEL_7_4-GCC-Linux-RHEL_7_4-X86_64.7z",
                "5.12.2-0-201903121858qtimageformats-Linux-RHEL_7_4-GCC-Linux-RHEL_7_4-X86_64.7z",
                "5.12.2-0-201903121858qtlocation-Linux-RHEL_7_4-GCC-Linux-RHEL_7_4-X86_64.7z",
                "5.12.2-0-201903121858qtmultimedia-Linux-RHEL_7_4-GCC-Linux-RHEL_7_4-X86_64.7z",
                "5.12.2-0-201903121858qtquickcontrols-Linux-RHEL_7_4-GCC-Linux-RHEL_7_4-X86_64.7z",
                "5.12.2-0-201903121858qtquickcontrols2-Linux-RHEL_7_4-GCC-Linux-RHEL_7_4-X86_64.7z",
                "5.12.2-0-201903121858qtremoteobjects-Linux-RHEL_7_4-GCC-Linux-RHEL_7_4-X86_64.7z",
                "5.12.2-0-201903121858qtscxml-Linux-RHEL_7_4-GCC-Linux-RHEL_7_4-X86_64.7z",
                "5.12.2-0-201903121858qtsensors-Linux-RHEL_7_4-GCC-Linux-RHEL_7_4-X86_64.7z",
                "5.12.2-0-201903121858qtserialbus-Linux-RHEL_7_4-GCC-Linux-RHEL_7_4-X86_64.7z",
                "5.12.2-0-201903121858qtserialport-Linux-RHEL_7_4-GCC-Linux-RHEL_7_4-X86_64.7z",
                "5.12.2-0-201903121858qtspeech-Linux-RHEL_7_4-GCC-Linux-RHEL_7_4-X86_64.7z",
                "5.12.2-0-201903121858qtsvg-Linux-RHEL_7_4-GCC-Linux-RHEL_7_4-X86_64.7z",
                "5.12.2-0-201903121858qttools-Linux-RHEL_7_4-GCC-Linux-RHEL_7_4-X86_64.7z",
                "5.12.2-0-201903121858qttranslations-Linux-RHEL_7_4-GCC-Linux-RHEL_7_4-X86_64.7z",
                "5.12.2-0-201903121858qtwayland-Linux-RHEL_7_4-GCC-Linux-RHEL_7_4-X86_64.7z",
                "5.12.2-0-201903121858qtwebchannel-Linux-RHEL_7_4-GCC-Linux-RHEL_7_4-X86_64.7z",
                "5.12.2-0-201903121858qtwebsockets-Linux-RHEL_7_4-GCC-Linux-RHEL_7_4-X86_64.7z",
                "5.12.2-0-201903121858qtwebview-Linux-RHEL_7_4-GCC-Linux-RHEL_7_4-X86_64.7z",
                "5.12.2-0-201903121858qtx11extras-Linux-RHEL_7_4-GCC-Linux-RHEL_7_4-X86_64.7z",
                "5.12.2-0-201903121858qtxmlpatterns-Linux-RHEL_7_4-GCC-Linux-RHEL_7_4-X86_64.7z"
            }.Select(x => baseUrl + x).ToList();
        }
        
        static Task Main(string[] args)
        {
            var options = ParseOptions<Options>(args);

            var linuxUrls = GetLinuxUrls();
            
            Target("download", () =>
            {
                var downloadsDirectory = ExpandPath("./downloads");
                if (!DirectoryExists(downloadsDirectory))
                {
                    Directory.CreateDirectory(downloadsDirectory);
                }
                foreach (var remoteFileUrl in linuxUrls)
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
                foreach (var remoteFileUrl in linuxUrls)
                {
                    var downloadedFilePath = Path.Combine(ExpandPath("./downloads"), Path.GetFileName(remoteFileUrl));
                    Info($"Extracting: {downloadedFilePath}");
                    RunShell($"cd \"{extractedDirectory}\" && 7za x {downloadedFilePath} -aoa");
                }
            });

            Target("default", DependsOn("download", "extract"));
            
            return Run(options);
        }

        // ReSharper disable ClassNeverInstantiated.Local
        class Options : RunnerOptions
        // ReSharper restore ClassNeverInstantiated.Local
        {
        }
    }
}
