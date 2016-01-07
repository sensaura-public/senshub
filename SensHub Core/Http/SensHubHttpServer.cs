using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IotWeb.Common;
using IotWeb.Common.Util;
using IotWeb.Common.Http;
using SensHub.Plugins;
using SensHub.Core;
using Splat;

namespace SensHub.Core.Http
{
	public class SensHubHttpServer : BaseHttpServer, IEnableLogger
	{
		// Prefix for static site resources
		private const string SitePrefix = "SensHub.Core.Resources.Site.";

		/// <summary>
		/// Create and set up the web server.
		/// </summary>
		/// <param name="server"></param>
		/// <param name="useStaticFiles"></param>
		/// <param name="unpackSite"></param>
		public SensHubHttpServer(ISocketServer server, bool useStaticFiles, bool unpackSite)
			: base(server)
		{
			// Get the root of the static site directory
			IFolder folder = Locator.Current.GetService<IFolder>();
			folder = folder.OpenFolder(ServiceManager.SiteFolder);
			// Unpack the site if we need to
			if (useStaticFiles && unpackSite)
			{
				// We want to serve content from a directory rather than from the resources and
				// we want to unpack our content from the embedded resources
				Assembly asm = Utilities.GetContainingAssembly<SensHubHttpServer>();
				foreach (string resourceName in asm.GetManifestResourceNames())
				{
					if (!resourceName.StartsWith(SitePrefix))
						continue;
					// Determine the target file name
					// TODO: There should be a better way to do this
					string targetFile = resourceName.Substring(SitePrefix.Length);
					targetFile = targetFile.Replace('.', '/');
					int index = targetFile.LastIndexOf('/');
					targetFile = targetFile.Substring(0, index) + "." + targetFile.Substring(index + 1);
					// Get the directory
					string directory = "";
					index = targetFile.LastIndexOf('/');
					IFolder target = folder;
					if (index >= 0)
					{
						directory = targetFile.Substring(0, index);
						targetFile = targetFile.Substring(index + 1);
						target = target.CreateChildren(directory);
					}
					// Now copy the resource
					Stream output = target.CreateFile(targetFile, FileAccessMode.ReadAndWrite, CreationOptions.ReplaceExisting);
					Stream input = asm.GetManifestResourceStream(resourceName);
					input.CopyTo(output);
					output.Dispose();
					input.Dispose();
				}
			}
			// Add the appropriate default handler
			if (useStaticFiles)
				AddHttpRequestHandler("/", new StaticHttpHandler(folder));
			else
				AddHttpRequestHandler("/", new HttpResourceHandler(Utilities.GetContainingAssembly<SensHubHttpServer>(), SitePrefix, "index.html"));
			// Add a static file handler for plugin provided images
			folder = Locator.Current.GetService<IFolder>();
			folder = folder.CreateChildren(ServiceManager.SiteFolder + ServiceManager.BaseImageUrl);
			AddHttpRequestHandler(ServiceManager.BaseImageUrl + "/", new StaticHttpHandler(folder));
			// TODO: Add the RPC request handler
		}
	}
}
