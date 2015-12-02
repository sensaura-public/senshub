using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SensHub.Plugins
{
	/// <summary>
	/// Utility class to define some well known topics
	/// </summary>
	public static class Topics
	{
		/// <summary>
		/// Server notifications.
		/// 
		/// This is the root for all server notifications. If you want to capture
		/// all of them subscribe to this.
		/// </summary>
		public const string ServerNotifications = "private/server/notifications";

		/// <summary>
		/// Server heartbeat
		/// 
		/// The server sends out a 'heartbeat' message approximately once a
		/// minute. This provides some basic statistical information about
		/// the server state and can also be used for simple timing operations.
		/// </summary>
		public const string ServerHeartbeat = ServerNotifications + "/heartbeat";

		/// <summary>
		/// Errors
		/// 
		/// Any errors that occur while the server is running are sent to this
		/// topic.
		/// </summary>
		public const string ServerErrors = ServerNotifications + "/errors";

		/// <summary>
		/// Warnings
		/// 
		/// Any warnings that occur while the server is running are sent to this
		/// topic.
		/// </summary>
		public const string ServerWarning = ServerNotifications + "/warnings";
	}
}
