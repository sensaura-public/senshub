using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IotWeb.Common.Http;

namespace SensHub.Core.Http
{
	class WebSocketRpcHandler : IWebSocketRequestHandler
	{
		// The protocol string we support
		private const string Protocol = "senshub";

		// The active set of connections
		private Dictionary<WebSocket, RpcConnection> m_connections = new Dictionary<WebSocket,RpcConnection>();

		/// <summary>
		/// Determine if we will accept the request.
		/// </summary>
		/// <param name="uri"></param>
		/// <param name="protocol"></param>
		/// <returns></returns>
		public bool WillAcceptRequest(string uri, string protocol)
		{
			return (uri.Length == 0) && (protocol == Protocol);
		}

		/// <summary>
		/// Called to notify of a new connection
		/// </summary>
		/// <param name="socket"></param>
		public void Connected(WebSocket socket)
		{
			socket.ConnectionClosed += OnConnectionClosed;
			RpcConnection connection = new RpcConnection(socket);
			lock (this)
			{
				m_connections.Add(socket, connection);
			}
		}

		/// <summary>
		/// Close the connection
		/// </summary>
		/// <param name="socket"></param>
		void OnConnectionClosed(WebSocket socket)
		{
			RpcConnection connection = null;
			lock (this)
			{
				if (!m_connections.ContainsKey(socket))
					return;
				connection = m_connections[socket];
				m_connections.Remove(socket);
			}
			connection.Close();
		}
	}
}
