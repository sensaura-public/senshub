using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SensHub.Plugins;

namespace SensHub.Server.Http
{
    public class HttpSession : ISubscriber
    {
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
			public IReadOnlyDictionary<string, object> Pack()
			{
				Dictionary<string, object> results = new Dictionary<string, object>();
				results.Add("timestamp", Timestamp);
				results.Add("topic", Topic.ToString());
				results.Add("payload", Payload);
				return (IReadOnlyDictionary<string, object>)results;
			}
		}

		// Instance variables
		private List<MessageInfo> m_messages = new List<MessageInfo>();

		/// <summary>
		/// Get the list of pending messages for this session.
		/// 
		/// Reading this property clears the list.
		/// </summary>
		public List<MessageInfo> Messages
		{
			get
			{
				List<MessageInfo> result = new List<MessageInfo>();
				lock (m_messages)
				{
					result.AddRange(m_messages);
					m_messages.Clear();
				}
				return result;
			}
		}

        /// <summary>
        /// Session ID
        /// </summary>
        public Guid UUID { get; private set; }

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
        public DateTime LastAccess { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
        internal HttpSession()
        {
            UUID = Guid.NewGuid();
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
		public void MessageReceived(ITopic topic, object source, Message message)
		{
			lock (m_messages)
			{
				m_messages.Add(new MessageInfo(topic, message));
			}
		}
	}
}
