using System;
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
            return url;
        }
    }
}
