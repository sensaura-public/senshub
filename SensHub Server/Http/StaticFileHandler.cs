using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SensHub.Server.Http
{
    internal class StaticFileHandler : HttpRequestHandler
    {
        private string m_path;

        public StaticFileHandler(string path)
        {
            m_path = path;
        }

        public override string HandleRequest(string url, HttpListenerRequest request, HttpListenerResponse response)
        {
            // Convert the URI into a full file system path
            string path = Path.GetFullPath(Path.Combine(m_path, url));
            if (path.Length < m_path.Length)
            {
                // Trying to do something tricky with indirect paths.
                return NotFound(response);
            }
            // Is it a directory ?
            if (Directory.Exists(path))
            {
                // Add a 'index.html' to it
                path = Path.Combine(path, "index.html");
            }
            // Does the file exist ?
            if (!File.Exists(path))
                return NotFound(response);
            // Set up the response
            FileInfo info = new FileInfo(path);
            response.ContentLength64 = info.Length;
            response.ContentType = MimeType.FromExtension(path);
            Stream input = File.Open(path, FileMode.Open);
            input.CopyTo(response.OutputStream);
            input.Close();
            // No additional content
            return null;
        }
    }
}
