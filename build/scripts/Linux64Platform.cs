using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using static Build.Buildary.Shell;
using static Build.Buildary.Directory;
using static Build.Buildary.File;
using static Build.Buildary.Path;

namespace Build
{
    public class Linux64Platform : IPlatform
    {
        public string QtVersion => "5.12.2";

        public string PlatformArch => "linux-x64";

        public string[] GetUrls()
        {
            var gccBase = "https://download.qt.io/online/qtsdkrepository/linux_x64/desktop/qt5_5122/qt.qt5.5122.gcc_64/";

            var urls = new List<string>
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
            }.Select(x => gccBase + x).ToList();
            
            urls.Add("https://download.qt.io/online/qtsdkrepository/linux_x64/desktop/qt5_5122/qt.qt5.5122.qtvirtualkeyboard.gcc_64/5.12.2-0-201903121858qtvirtualkeyboard-Linux-RHEL_7_4-GCC-Linux-RHEL_7_4-X86_64.7z");

            return urls.ToArray();
        }

        public void PackageDev(string extractedDirectory, string destination)
        {
            RunShell($"cd \"{extractedDirectory}/{QtVersion}/gcc_64\" && tar -cvzpf {destination} *");
        }

        public void PackageRuntime(string extractedDirectory, string destination)
        {
            // Clean up the tmp directory in prep for a runtime 
            // First get a list of all dependencies from every .so files.
            var linkedFiles = new List<string>();
            foreach(var file in GetFiles(extractedDirectory, pattern:"*.so*", recursive:true))
            {
                var lddOutput = ReadShell($"ldd {file}");
                foreach (var _line in lddOutput.Split(Environment.NewLine))
                {
                    var line = _line.TrimStart('\t').TrimStart('\n');
                    var match = Regex.Match(line, @"(.*) =>.*");
                    if (match.Success)
                    {
                        var linkedFile = match.Groups[1].Value;
                        if(!linkedFiles.Contains(linkedFile))
                        {
                            linkedFiles.Add(linkedFile);
                        }
                    }
                }
            }
            
            // Let's remove any file from lib/ that isn't linked against anything.
            foreach(var file in GetFiles(extractedDirectory, recursive:true))
            {
                var fileName = Path.GetFileName(file);
                if (!linkedFiles.Contains(fileName))
                {
                    DeleteFile(file);
                }
            }

            foreach (var directory in GetDirecories(extractedDirectory, recursive: true))
            {
                if (!DirectoryExists(directory))
                {
                    continue;
                }
                
                var directoryName = Path.GetFileName(directory);
                
                if (directoryName == "cmake")
                {
                    DeleteDirectory(directory);
                }

                if (directoryName == "pkgconfig")
                {
                    DeleteDirectory(directory);
                }
            }
            
            foreach (var file in GetFiles(extractedDirectory, recursive: true))
            {
                var fileName = Path.GetFileName(file);
                var fileExtension = Path.GetExtension(fileName);
                
                if (fileExtension == ".qmlc")
                {
                    DeleteFile(file);
                }
            }
            
            RunShell($"cd \"{extractedDirectory}/{QtVersion}/gcc_64\" && tar -cvzpf {destination} *");
        }
    }
}