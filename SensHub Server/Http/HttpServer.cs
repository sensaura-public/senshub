using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SensHub.Plugins;
using SensHub.Server;
using Splat;
using System.Reflection;

namespace SensHub.Server.Http
{
    /// <summary>
    /// Implements a simple single threaded HTTP server to provide the UI.
    /// </summary>
    public class HttpServer
    {
        // The directory containing the site
        private string m_sitePath;

        public HttpServer(string sitePath)
        {
            m_sitePath = sitePath;
        }

        /// <summary>
        /// Convert a resource name (without the prefix) into a target
        /// path and file. Assumes that all files have a single suffix
        /// (eg .html, .css, etc) and all other dots in the name are
        /// replaced with a directory separator.
        /// </summary>
        /// <param name="resourceName"></param>
        /// <returns></returns>
        private string GetTargetFileName(string resourceName)
        {
            int lastDot = resourceName.LastIndexOf('.');
            return resourceName.Substring(0, lastDot).Replace('.', '/') + resourceName.Substring(lastDot);
        }

        /// <summary>
        /// Unpack the site into a static directory.
        /// 
        /// In a 'production' environment the server is fronted by an Nginx
        /// instance running as a caching proxy. To facilitate this we unpack
        /// the site from resources to a directory so it can access them.
        /// </summary>
        /// <param name="siteDir"></param>
        public void UnpackSite()
        {
            // Make sure we have an empty directory to start with
            Directory.Delete(m_sitePath, true);
            Directory.CreateDirectory(m_sitePath);
            // Walk through all the resources
            var assembly = Assembly.GetExecutingAssembly();
            string prefix = assembly.GetName().Name + ".Resources.";
            foreach (var resourceName in assembly.GetManifestResourceNames())
            {
                if (!resourceName.StartsWith(prefix))
                    continue;
                string fileName = GetTargetFileName(resourceName.Substring(prefix.Length));
                // Make sure the directory exists
                string directory = Path.Combine(m_sitePath, Path.GetDirectoryName(fileName));
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
                // Copy the resource in
                using (Stream source = assembly.GetManifestResourceStream(resourceName))
                {
                    Stream target = File.Create(Path.Combine(m_sitePath, fileName));
                    source.CopyTo(target);
                    target.Close();
                }
            }
        }
    }
}
