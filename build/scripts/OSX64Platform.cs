using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using static Build.Buildary.Shell;
using static Build.Buildary.Directory;
using static Build.Buildary.File;

namespace Build
{
    public class OSX64Platform : IPlatform
    {
        public string QtVersion => "5.12.2";

        public string PlatformArch => "osx-x64";

        public string[] GetUrls()
        {
            return Helpers.GetQtArchives("https://download.qt.io/online/qtsdkrepository/mac_x64/desktop/qt5_5122/",
                "qt.qt5.5122.clang_64",
                "qt.qt5.5122.qtvirtualkeyboard.clang_64");
        }

        public void PackageDev(string extractedDirectory, string destination, string version)
        {
            extractedDirectory = Path.Combine(extractedDirectory, QtVersion, "clang_64");
            File.WriteAllText(Path.Combine(extractedDirectory, "version.txt"), version);
            
            RunShell($"cd \"{extractedDirectory}\" && tar -cvzpf \"{destination}\" *");
        }

        public void PackageRuntime(string extractedDirectory, string destination, string version)
        {
            extractedDirectory = Path.Combine(extractedDirectory, QtVersion, "clang_64");
            File.WriteAllText(Path.Combine(extractedDirectory, "version.txt"), version);
            
            foreach (var directory in GetDirecories(extractedDirectory))
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
            
            foreach (var directory in GetDirecories(extractedDirectory, recursive:true))
            {
                if (!DirectoryExists(directory))
                {
                    continue;
                }
                
                var directoryName = Path.GetFileName(directory);
                if (directoryName == "Headers")
                {
                    DeleteDirectory(directory);
                    continue;
                }
                            
                if (directoryName.EndsWith(".dSYM"))
                {
                    DeleteDirectory(directory);
                    continue;
                }

                if (directory == "cmake")
                {
                    DeleteDirectory(directory);
                    continue;
                }

                if (directory == "pkgconfig")
                {
                    DeleteDirectory(directory);
                }
            }
            
            foreach (var file in GetFiles(extractedDirectory, recursive:true))
            {
                var extension = Path.GetExtension(file);
                var fileName = Path.GetFileNameWithoutExtension(file);
                            
                if (fileName.EndsWith("_debug"))
                {
                    DeleteFile(file);
                    continue;
                }
                            
                if (extension == ".prl")
                {
                    DeleteFile(file);
                    continue;
                }
                            
                if (extension == ".plist")
                {
                    DeleteFile(file);
                    continue;
                }

                if (extension == ".qmlc")
                {
                    DeleteFile(file);
                    continue;
                }

                if (extension == ".cmake")
                {
                    DeleteFile(file);
                    continue;
                }

                if (extension == ".a")
                {
                    DeleteFile(file);
                    continue;
                }

                if (extension == ".la")
                {
                    DeleteFile(file);
                }
            }
            
            RunShell($"cd \"{extractedDirectory}\" && tar -cvzpf \"{destination}\" *");
        }
    }
}