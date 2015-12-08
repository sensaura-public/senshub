using System;
using System.Net;
using System.Net.WebSockets;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SensHub.Server.Http
{
	public abstract class WebSocketRequestHandler : HttpRequestHandler
	{
		public abstract string Protocol { get; }

		public abstract void AttachWebSocket(string uri, WebSocket socket);

		public abstract bool WillAcceptWebSocket(string uri);
	}
}
