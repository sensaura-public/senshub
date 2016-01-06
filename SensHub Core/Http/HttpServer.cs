using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IotWeb.Common;
using IotWeb.Common.Http;

namespace SensHub.Core.Http
{
	public class HttpServer : BaseHttpServer
	{
		public HttpServer(ISocketServer server)
			: base(server)
		{
			// Add our handlers
		}
	}
}
