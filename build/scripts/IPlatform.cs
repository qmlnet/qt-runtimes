namespace Build
{
    public interface IPlatform
    {
        string QtVersion { get; }
        
        string PlatformArch { get; }
        
        string[] GetUrls();

        void PackageDev(string extractedDirectory, string destination, string version);

        void PackageRuntime(string extractedDirectory, string destination, string version);
    }
}