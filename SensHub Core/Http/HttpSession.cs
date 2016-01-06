using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IotWeb.Common.Http;
using SensHub.Plugins;
using SensHub.Core.Utils;
using SensHub.Core.Messages;
using Splat;

namespace SensHub.Core.Http
{
    public class HttpSession : ISubscriber, IEnableLogger
    {
		// The lifetime of a session (in minutes)
		public static int SessionLifetime = 10;

		/// <summary>
		/// Hold information about a message.
		/// </summary>
		public class MessageInfo : IPackable
		{
			public DateTime Timestamp { get; private set; }

			public ITopic Topic { get; private set; }

			public Message Payload { get; private set; }

			/// <summary>
			/// Constructor with topic and payload.
			/// </summary>
			/// <param name="topic"></param>
			/// <param name="payload"></param>
			public MessageInfo(ITopic topic, Message payload)
			{
				Timestamp = DateTime.Now;
				Topic = topic;
				Payload = payload;
			}

			/// <summary>
			/// Get a packed version of the message information.
			/// </summary>
			/// <returns></returns>
			public IDictionary<string, object> Pack()
			{
				Dictionary<string, object> results = new Dictionary<string, object>();
				results.Add("timestamp", Timestamp);
				results.Add("topic", Topic.ToString());
				results.Add("payload", Payload);
				return (IDictionary<string, object>)results;
			}
		}

		// Instance variables
		private List<MessageInfo> m_messages = new List<MessageInfo>();

        public WebSocket Socket { get; set; }

		/// <summary>
		/// Get the list of pending messages for this session.
		/// 
		/// Reading this property clears the list.
		/// </summary>
		public List<IDictionary<string, object>> Messages
		{
			get
			{
				List<IDictionary<string, object>> result = new List<IDictionary<string, object>>();
				lock (m_messages)
				{
					foreach (MessageInfo info in m_messages)
						result.Add(info.Pack());
					m_messages.Clear();
				}
				return result;
			}
		}

        /// <summary>
        /// Session ID
        /// </summary>
        public string ID { get; private set; }

        /// <summary>
        /// Session variables.
        /// </summary>
        public Dictionary<string, object> Variables { get; private set; }

        /// <summary>
        /// If this session has been authenticated
        /// </summary>
        public bool Authenticated { get; set; }

        /// <summary>
        /// The client address associated with the session
        /// </summary>
        public string RemoteAddress { get; set; }

        /// <summary>
        /// When the session was last accessed
        /// </summary>
        public DateTime LastAccess { get; private set; }

		/// <summary>
		/// Constructor
		/// </summary>
        private HttpSession()
        {
            ID = Guid.NewGuid().ToString();
			LastAccess = DateTime.Now;
            Variables = new Dictionary<string, object>();
        }

		/// <summary>
		/// Handle incoming messages.
		/// 
		/// When a web client subscribes to a topic it is the session
		/// that acts as the subscriber. It simply stores all messages
		/// received in an internal list, the RpcRequestHandler will
		/// send the list back with any response it sends.
		/// </summary>
		/// <param name="topic"></param>
		/// <param name="source"></param>
		/// <param name="message"></param>
		public async void MessageReceived(ITopic topic, object source, Message message)
		{
            MessageInfo info = new MessageInfo(topic, message);
            if (Socket == null)
            {
                lock (m_messages)
                    m_messages.Add(info);
                return;
            }
			// TODO: Send the message over the socket connection
		}

		#region Static interface
		// Set of active sessions
		private static Dictionary<string, HttpSession> m_sessions = new Dictionary<string, HttpSession>();

		// When we last checked for expired sessions
		private static DateTime m_lastExpiryCheck = DateTime.Now;

		/// <summary>
		/// Expire any sessions that have timed out
		/// </summary>
		private static void ExpireSessions()
		{
			lock (m_sessions)
			{
				// Do we need to do a check ?
				DateTime now = DateTime.Now;
				if ((now - m_lastExpiryCheck).TotalMinutes < (SessionLifetime / 2))
					return;
				m_lastExpiryCheck = DateTime.Now;
				// Build a list of expired sessions
				List<string> expired = null;
				foreach (string id in m_sessions.Keys)
				{
					HttpSession session = m_sessions[id];
					if ((now - session.LastAccess).TotalMinutes > SessionLifetime)
					{
						if (expired == null)
							expired = new List<string>();
						expired.Add(id);
					}
				}
                // Did we find any?
                if (expired == null)
                    return;
                // Get rid of any that are no longer active
                MessageBus mb = Locator.Current.GetService<MessageBus>();
				foreach (string id in expired)
                {
                    mb.Unsubscribe(m_sessions[id]);
                    m_sessions.Remove(id);
                }
            }
		}

		/// <summary>
		/// Get a session associated with the given ID
		/// </summary>
		/// <param name="id"></param>
		/// <returns>The session for the ID or null if none is available.</returns>
		public static HttpSession GetSession(string id)
		{
			// Run an expiry check first
			ExpireSessions();
			// See if we have a session with that ID
			HttpSession result = null;
			lock (m_sessions)
			{
				if (m_sessions.ContainsKey(id))
				{
					result = m_sessions[id];
					result.LastAccess = DateTime.Now;
				}
			}
			return result;
		}

		/// <summary>
		/// Create a new session
		/// </summary>
		/// <returns>A session</returns>
		public static HttpSession CreateSession()
		{
			// Run an expiry check first
			ExpireSessions();
			// Create the new session and add it to the set
			HttpSession session = new HttpSession();
			lock (m_sessions)
			{
				m_sessions[session.ID] = session;
			}
			return session;
		}
		#endregion
	}
}
