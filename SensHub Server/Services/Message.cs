using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SensHub.Server.Services
{
	/// <summary>
	/// Represents a single message posted on the MessageBus. Once published
	/// a message is immutable so it is represented as an implementation of
	/// IReadOnlyDictionary<string, object>
	/// </summary>
	public class Message : IReadOnlyDictionary<string, Object>
	{
		private Dictionary<string, Object> m_payload;

		public Message()
		{
		}

		public Message(Dictionary<string, Object> payload)
		{
			// Create a copy of the dictionary
			m_payload = new Dictionary<string, object>(payload);
		}

		public bool ContainsKey(string key)
		{
			if (m_payload == null)
				return false;
			return m_payload.ContainsKey(key);
		}

		public IEnumerable<string> Keys
		{
			get 
			{
				if (m_payload == null)
					return Enumerable.Empty<string>();
				return m_payload.Keys;
			}
		}

		public bool TryGetValue(string key, out object value)
		{
			if (m_payload == null)
			{
				value = null;
				return false;
			}
			return m_payload.TryGetValue(key, out value);
		}

		public IEnumerable<object> Values
		{
			get 
			{
				if (m_payload == null)
					return Enumerable.Empty<object>();
				return m_payload.Values;
			}
		}

		public object this[string key]
		{
			get 
			{
				if (m_payload == null)
					throw new KeyNotFoundException();
				return m_payload[key];
			}
		}

		public int Count
		{
			get 
			{
				if (m_payload == null)
					return 0;
				return m_payload.Count;
			}
		}

		public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
		{
			if (m_payload == null)
				return Enumerable.Empty<KeyValuePair<string, object>>().GetEnumerator();
			return m_payload.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			if (m_payload == null)
				return Enumerable.Empty<KeyValuePair<string, object>>().GetEnumerator();
			return m_payload.GetEnumerator();
		}
	}
}
