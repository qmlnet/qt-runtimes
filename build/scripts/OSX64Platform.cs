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
    public class OSX64Platform : IPlatform
    {
        public string QtVersion => "5.12.2";

        public string PlatformArch => "osx-x64";

        public string[] GetUrls()
        {
            var urls = Helpers.GetQtArchives("https://download.qt.io/online/qtsdkrepository/mac_x64/desktop/qt5_5122",
                    "qt.qt5.5122.clang_64",
                    "qt.qt5.5122.qtvirtualkeyboard.clang_64")
                .ToList();
            
            urls.AddRange(Helpers.GetQtArchives("https://download.qt.io/online/qtsdkrepository/mac_x64/desktop/tools_qtcreator",
                "qt.tools.qtcreator"));

            return urls.ToArray();
        }

        private void Patch(string extractedDirectory)
        {
            RunShell($"mv \"{extractedDirectory}/{QtVersion}/clang_64\" \"{extractedDirectory}/qt\"");
            DeleteDirectory($"{extractedDirectory}/{QtVersion}");
            
            using (var fileStream = File.OpenWrite(Path.Combine(extractedDirectory, "qt", "bin", "qt.conf")))
            {
                using (var streamWriter = new StreamWriter(fileStream))
                {
                    streamWriter.WriteLine("[Paths]");
                    streamWriter.WriteLine("Prefix=..");
                }
            }
            
            using(var fileStream = File.Open(Path.Combine(extractedDirectory, "qt", "mkspecs", "qconfig.pri"), FileMode.Append))
            {
                using (var streamWriter = new StreamWriter(fileStream))
                {
                    streamWriter.WriteLine("QT_EDITION = OpenSource");
                    streamWriter.WriteLine("QT_LICHECK =");
                }
            }
        }

        public void PackageDev(string extractedDirectory, string destination, string version)
        {
            Patch(extractedDirectory);
            
            File.WriteAllText(Path.Combine(extractedDirectory, "version.txt"), version);
            
            RunShell($"cd \"{extractedDirectory}\" && tar -cvzpf \"{destination}\" *");
        }

        public void PackageRuntime(string extractedDirectory, string destination, string version)
        {
            Patch(extractedDirectory);
            
            File.WriteAllText(Path.Combine(extractedDirectory, "version.txt"), version);
            
            DeleteDirectory(Path.Combine(extractedDirectory, "Qt Creator.app"));
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
            
            foreach (var directory in GetDirecories(Path.Combine(extractedDirectory, "qt"), recursive:true))
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
            
            foreach (var file in GetFiles(Path.Combine(extractedDirectory, "qt"), recursive:true))
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