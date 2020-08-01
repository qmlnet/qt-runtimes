using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using static Build.Buildary.Shell;
using static Build.Buildary.Directory;
using static Build.Buildary.File;

namespace Build
{
    public class OSX64Platform : IPlatform
    {
        public string QtVersion => Helpers.QtVersion;

        public string PlatformArch => "osx-x64";

        public string[] GetUrls()
        {
            var urls = Helpers.GetQtArchives($"https://download.qt.io/online/qtsdkrepository/mac_x64/desktop/{Helpers.QtVersionURL}",
                    $"{Helpers.QtVersionURLWithDot}.clang_64",
                    $"{Helpers.QtVersionURLWithDot}.qtvirtualkeyboard.clang_64")
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
            
            // These are libraries that need to be loaded first, before loading libQmlNet.so.
            // It is used by the RuntimeManager when manually loading a Qt environment.
            var preLoadText = new StringBuilder();
            preLoadText.AppendLine("QtQuickControls2.framework/Versions/5/QtQuickControls2");
            preLoadText.AppendLine("QtQuick.framework/Versions/5/QtQuick");
            preLoadText.AppendLine("QtWidgets.framework/Versions/5/QtWidgets");
            preLoadText.AppendLine("QtGui.framework/Versions/5/QtGui");
            preLoadText.AppendLine("QtQml.framework/Versions/5/QtQml");
            preLoadText.AppendLine("QtNetwork.framework/Versions/5/QtNetwork");
            preLoadText.AppendLine("QtTest.framework/Versions/5/QtTest");
            preLoadText.AppendLine("QtCore.framework/Versions/5/QtCore");
            File.WriteAllText(Path.Combine(extractedDirectory, "qt", "lib", "preload.txt"), preLoadText.ToString());
        }

        public void PackageDev(string extractedDirectory, string destination, string version)
        {
            Patch(extractedDirectory);
            
            File.WriteAllText(Path.Combine(extractedDirectory, "version.txt"), version);
            
            Helpers.AssertValidSymlinks(extractedDirectory);
            RunShell($"cd \"{extractedDirectory}\" && tar -czpf \"{destination}\" *");
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
            
            Helpers.AssertValidSymlinks(extractedDirectory);
            RunShell($"cd \"{extractedDirectory}\" && tar -czpf \"{destination}\" *");
        }
    }
}