using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sensaura.MessageBus
{
	/// <summary>
	/// Represents a single message posted on the MessageBus
	/// </summary>
	public class Message
	{
		/// <summary>
		/// Helper class to construct messages
		/// </summary>
		public class Builder
		{
			// Message state
			public Guid? Source { get; set; }
			public Guid? Target { get; set; }
			private Dictionary<string, Object> m_payload;
			private Dictionary<string, Uri> m_attachments;

			public Builder()
			{
				m_payload = new Dictionary<string, object>();
				m_attachments = new Dictionary<string, Uri>();
			}

			/// <summary>
			/// Clear the current values and prepare for building a new message.
			/// </summary>
			public void Clear()
			{
				m_payload.Clear();
				m_attachments.Clear();
				Source = null;
				Target = null;
			}

			/// <summary>
			/// Populate the values with the contents of an existing message.
			/// </summary>
			/// <param name="message"></param>
			public void Copy(Message message)
			{
				Clear();
				Source = message.Source;
				Target = message.Target;
				foreach (string key in message.Payload.Keys) 
				{
					Object value;
					if (message.Payload.TryGetValue(key, out value))
						m_payload.Add(key, value);
				}
				foreach (string key in message.Attachments.Keys)
				{
					Uri value;
					if (message.Attachments.TryGetValue(key, out value))
						m_attachments.Add(key, value);
				}
			}

			/// <summary>
			/// Create a message instance with the current values.
			/// </summary>
			/// <returns></returns>
			public Message CreateMessage()
			{
				Message message = new Message(
					Source,
					Target,
					new Dictionary<string, object>(m_payload),
					new Dictionary<string, Uri>(m_attachments)
					);
				Clear();
				return message;
			}
		}

		#region Properties
		private Guid? m_source;
		public Guid? Source
		{
			get { return m_source; }
		}

		private Guid? m_target;
		public Guid? Target
		{
			get { return m_target; }
		}

		private IReadOnlyDictionary<string, Object> m_payload;
		public IReadOnlyDictionary<string, Object> Payload
		{
			get { return m_payload; }
		}

		private IReadOnlyDictionary<string, Uri> m_attachments;
		public IReadOnlyDictionary<string, Uri> Attachments
		{
			get { return m_attachments; }
		}
		#endregion

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="source"></param>
		/// <param name="target"></param>
		/// <param name="payload"></param>
		/// <param name="attachments"></param>
		internal Message(Guid? source, Guid? target, Dictionary<string, Object> payload, Dictionary<string, Uri> attachments)
		{
			m_source = source;
			m_target = target;
			// Make sure we have empty dictionaries, not null
			payload = (payload == null) ? new Dictionary<string, Object>() : payload;
			m_payload = new ReadOnlyDictionary<string, Object>(payload);
			attachments = (attachments == null) ? new Dictionary<string, Uri>() : attachments;
			m_attachments = new ReadOnlyDictionary<string, Uri>(attachments);
		}
	}
}
