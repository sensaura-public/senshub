using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SensHub.Server.Services
{
	public class MessageBuilder
	{
		private Dictionary<string, object> m_payload;

		public MessageBuilder()
		{
			m_payload = new Dictionary<string, object>();
		}

		public void Clear()
		{
			m_payload.Clear();
		}

		public void Add(string key, object value)
		{
			m_payload.Add(key, value);
		}

		public Message CreateMessage()
		{
			Message message = new Message(m_payload);
			Clear();
			return message;
		}

		public void CopyFrom(Message message)
		{
			Clear();
			foreach (string key in message.Keys)
				m_payload.Add(key, message[key]);
		}
	}
}
