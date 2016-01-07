using System;
using System.IO;
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
			// Split into path and filename
			int index = uri.LastIndexOf('/');
			string path = (index < 0) ? "" : uri.Substring(0, index);
			string filename = (index < 0) ? uri : uri.Substring(index + 1);
			// Use default filename if applicable
			if (filename.Length == 0)
				filename = m_defaultFile;
			// Strip leading separators from URI
			if (path.StartsWith("/"))
				path = path.Substring(1);
			// Now look for the file
			IFolder folder = m_basePath.OpenChild(path);
			if (folder == null)
				throw new HttpNotFoundException();
			if (!folder.FileExists(filename))
				throw new HttpNotFoundException();
			// Get the content type
			response.ResponseCode = HttpResponseCode.Ok;
			response.ContentType = MimeType.FromExtension(filename);
			Stream input = folder.CreateFile(filename, FileAccessMode.Read, CreationOptions.OpenIfExists);
			input.CopyTo(response.Content);
			input.Dispose();
		}
	}
}
