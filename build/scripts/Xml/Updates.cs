using System.Collections.Generic;
using System.Xml.Serialization;

namespace Build.Xml
{
    public class Updates
    {
        [XmlElement]
        public string ApplicationName { get; set; }
        
        [XmlElement]
        public string ApplicationVersion { get; set; }
        
        [XmlElement]
        public bool Checksum { get; set; }
        
        [XmlElement("PackageUpdate")]
        public List<PackageUpdate> PackageUpdates { get; set; }
    }

    public class PackageUpdate
    {
        [XmlElement]
        public string Name { get; set; }
        
        [XmlElement]
        public string Version { get; set; }
        
        [XmlElement]
        public string Dependencies { get; set; }
        
        [XmlElement]
        public string DownloadableArchives { get; set; }
    }
}