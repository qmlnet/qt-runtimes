using System.IO;
using static Build.Buildary.Shell;
using static Build.Buildary.Directory;
using static Build.Buildary.File;
using static Build.Buildary.Path;

namespace Build
{
    public class Windows64Platform : IPlatform
    {
        public string QtVersion => "5.12.2";

        public string PlatformArch => "windows-64";
        
        public string[] GetUrls()
        {
            return Helpers.GetQtArchives("https://download.qt.io/online/qtsdkrepository/windows_x86/desktop/qt5_5122",
                "qt.qt5.5122.win64_msvc2017_64",
                "qt.qt5.5122.qtvirtualkeyboard.win64_msvc2017_64");
        }

        public void PackageDev(string extractedDirectory, string destination, string version)
        {
            extractedDirectory = Path.Combine(extractedDirectory, QtVersion, "msvc2017_64");
            File.WriteAllText(Path.Combine(extractedDirectory, "version.txt"), version);
            
            RunShell($"cd \"{extractedDirectory}\" && tar -cvzpf \"{destination}\" *");
        }

        public void PackageRuntime(string extractedDirectory, string destination, string version)
        {
            extractedDirectory = Path.Combine(extractedDirectory, QtVersion, "msvc2017_64");
            File.WriteAllText(Path.Combine(extractedDirectory, "version.txt"), version);
            
            foreach (var directory in GetDirecories(extractedDirectory))
            {
                switch (Path.GetFileName(directory))
                {
                    case "bin":
                        // Delete everything that isn't a .dll in here.
                        foreach (var file in GetFiles(directory))
                        {
                            if (Path.GetExtension(file) != ".dll")
                            {
                                DeleteFile(file);
                            }
                        }
                        break;
                    case "qml":
                    case "plugins":
                        break;
                    default:
                        DeleteDirectory(directory);
                        break;
                }
            }
            
            // The windows build currently brings in all the .dll's for packaging.
            // However, it also brings in the *d.dll/*.pdb files. Let's remove them.
            foreach(var file in GetFiles(extractedDirectory, recursive:true))
            {
                if (file.EndsWith("d.dll"))
                {
                    if(FileExists(file.Substring(0, file.Length - 5) + ".dll"))
                    {
                        // This is a debug dll.
                        DeleteFile(file);
                    }
                }
                else if (file.EndsWith(".pdb"))
                {
                    DeleteFile(file);
                }
                else if (file.EndsWith("*.qmlc"))
                {
                    DeleteFile(file);
                }
            }
            
            RunShell($"cd \"{extractedDirectory}\" && tar -cvzpf \"{destination}\" *");
        }
    }
}