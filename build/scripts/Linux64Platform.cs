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
    public class Linux64Platform : IPlatform
    {
        public string QtVersion => Helpers.QtVersion;

        public string PlatformArch => "linux-x64";

        public string[] GetUrls()
        {
            var urls = Helpers.GetQtArchives($"https://download.qt.io/online/qtsdkrepository/linux_x64/desktop/{Helpers.QtVersionURL}",
                    $"{Helpers.QtVersionURLWithDot}.gcc_64",
                    $"{Helpers.QtVersionURLWithDot}.qtvirtualkeyboard.gcc_64")
                .ToList();
            
            urls.AddRange(Helpers.GetQtArchives("https://download.qt.io/online/qtsdkrepository/linux_x64/desktop/tools_qtcreator",
                    "qt.tools.qtcreator"));

            return urls.ToArray();
        }
        
        private void Patch(string extractedDirectory)
        {
            RunShell($"mv \"{extractedDirectory}/{QtVersion}/gcc_64\" \"{extractedDirectory}/qt\"");
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
            preLoadText.AppendLine("libQt5QuickControls2.so.5");
            preLoadText.AppendLine("libQt5Widgets.so.5");
            preLoadText.AppendLine("libQt5Gui.so.5");
            preLoadText.AppendLine("libQt5Qml.so.5");
            preLoadText.AppendLine("libQt5Core.so.5");
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
            
            DeleteDirectory(Path.Combine(extractedDirectory, "Tools"));
            DeleteDirectory(Path.Combine(extractedDirectory, "qt", "lib", "cmake"));
            DeleteDirectory(Path.Combine(extractedDirectory, "qt", "lib", "pkgconfig"));
            foreach (var directory in GetDirectories(Path.Combine(extractedDirectory, "qt")))
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
            
            foreach (var file in GetFiles(Path.Combine(extractedDirectory, "qt"), recursive: true))
            {
                var fileName = Path.GetFileName(file);
                var fileExtension = Path.GetExtension(fileName);
                
                if (fileExtension == ".qmlc")
                {
                    DeleteFile(file);
                }

                if (fileExtension == ".la")
                {
                    DeleteFile(file);
                }

                if (fileExtension == ".prl")
                {
                    DeleteFile(file);
                }

                if (fileExtension == ".a")
                {
                    DeleteFile(file);
                }
            }

            Helpers.AssertValidSymlinks(extractedDirectory);
            RunShell($"cd \"{extractedDirectory}\" && tar -czpf \"{destination}\" *");
        }
    }
}