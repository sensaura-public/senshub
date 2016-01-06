using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IotWeb.Common.Http;
using SensHub.Plugins;

namespace SensHub.Core.Http
{
	class StaticHttpHandler : IHttpRequestHandler
	{
		private IFolder m_basePath;
		private string m_defaultFile;

		/// <summary>
		/// Constructor with a base path and a default index file
		/// </summary>
		/// <param name="path"></param>
		/// <param name="defaultFile"></param>
		public StaticHttpHandler(IFolder path, string defaultFile = "index.html")
		{
			m_basePath = path;
			m_defaultFile = defaultFile;
		}

		/// <summary>
		/// Handle the request
		/// </summary>
		/// <param name="uri"></param>
		/// <param name="request"></param>
		/// <param name="response"></param>
		/// <param name="context"></param>
		public void HandleRequest(string uri, HttpRequest request, HttpResponse response, HttpContext context)
		{
			throw new NotImplementedException();
		}
	}
}
