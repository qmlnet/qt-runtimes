using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using static Build.Buildary.Shell;
using static Build.Buildary.Directory;
using static Build.Buildary.File;

namespace Build
{
    public class Linux64Platform : IPlatform
    {
        public string QtVersion => "5.12.2";

        public string PlatformArch => "linux-x64";

        public string[] GetUrls()
        {
            var urls = Helpers.GetQtArchives("https://download.qt.io/online/qtsdkrepository/linux_x64/desktop/qt5_5122",
                    "qt.qt5.5122.gcc_64",
                    "qt.qt5.5122.qtvirtualkeyboard.gcc_64")
                .ToList();
            
            urls.AddRange(Helpers.GetQtArchives("https://download.qt.io/online/qtsdkrepository/linux_x64/desktop/tools_qtcreator",
                    "qt.tools.qtcreator"));

            return urls.ToArray();
        }

        public void PackageDev(string extractedDirectory, string destination, string version)
        {
            RunShell($"mv \"{extractedDirectory}/{QtVersion}/gcc_64\" \"{extractedDirectory}/qt\"");
            DeleteDirectory($"{extractedDirectory}/{QtVersion}");
            
            File.WriteAllText(Path.Combine(extractedDirectory, "version.txt"), version);
            
            RunShell($"cd \"{extractedDirectory}\" && tar -cvzpf \"{destination}\" *");
        }

        public void PackageRuntime(string extractedDirectory, string destination, string version)
        {
            RunShell($"mv \"{extractedDirectory}/{QtVersion}/gcc_64\" \"{extractedDirectory}/qt\"");
            DeleteDirectory($"{extractedDirectory}/{QtVersion}");

            File.WriteAllText(Path.Combine(extractedDirectory, "version.txt"), version);
            
            DeleteDirectory(Path.Combine(extractedDirectory, "Tools"));
            foreach (var directory in GetDirecories(Path.Combine(extractedDirectory, "qt")))
            {
                switch (Path.GetFileName(directory))
                {
                    case "lib":
                    case "qml":
                    case "plugins":
                        break;
                    default:
                        DeleteDirectory(directory);
                        break;
                }
            }
            
            // Clean up the tmp directory in prep for a runtime 
            // First get a list of all dependencies from every .so files.
            var linkedFiles = new List<string>();
            foreach(var file in GetFiles(Path.Combine(extractedDirectory, "qt"), pattern:"*.so*", recursive:true))
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
            foreach(var file in GetFiles(Path.Combine(extractedDirectory, "qt", "lib"), recursive:true))
            {
                var fileName = Path.GetFileName(file);
                if (!linkedFiles.Contains(fileName))
                {
                    DeleteFile(file);
                }
            }

            foreach (var directory in GetDirecories(Path.Combine(extractedDirectory, "qt"), recursive: true))
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
            
            foreach (var file in GetFiles(Path.Combine(extractedDirectory, "qt"), recursive: true))
            {
                var fileName = Path.GetFileName(file);
                var fileExtension = Path.GetExtension(fileName);
                
                if (fileExtension == ".qmlc")
                {
                    DeleteFile(file);
                }
            }
            
            RunShell($"cd \"{extractedDirectory}\" && tar -cvzpf \"{destination}\" *");
        }
    }
}