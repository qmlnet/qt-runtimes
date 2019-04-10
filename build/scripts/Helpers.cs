using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Xml.Serialization;
using Build.Xml;

namespace Build
{
    public class Helpers
    {
        public static string GetResponse(string url)
        {
            using (var client = new HttpClient())
            {
                return client.GetStringAsync(url).GetAwaiter().GetResult();
            }
        }

        public static string[] GetQtArchives(string url, params string[] packages)
        {
            url = url.TrimEnd('/');
            
            var xml = GetResponse(
                $"{url}/Updates.xml");
            
            var serializer = new XmlSerializer(typeof(Updates));

            Updates updates;
            using (var memoryStream = new MemoryStream(UTF8Encoding.UTF8.GetBytes(xml)))
            {
                updates = (Updates) serializer.Deserialize(memoryStream);
            }

            var result = new List<string>();
            
            foreach (var package in packages)
            {
                var packageUpdate = updates.PackageUpdates.SingleOrDefault(x => x.Name == package);
                if (packageUpdate == null)
                {
                    throw new Exception($"Invalid package update {package}");
                }
                
                result.AddRange(packageUpdate.DownloadableArchives.Split(',').Select(x => x.Trim())
                    .Select(x =>
                        $"{url}/{packageUpdate.Name}/{packageUpdate.Version}{x}")
                    .ToList());
            }
            
            return result.ToArray();
        }
    }
}