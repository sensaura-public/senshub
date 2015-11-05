using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sensaura.Utilities;
using Sensaura.MessageBus;

namespace Sensaura.Configuration
{
	public class Configuration : BaseDictionary<string, string>, IJsonSerialisable
	{
		private static MessageBuilder s_builder = new MessageBuilder();

		private string m_name;
		private bool m_dirty;
		private Topic m_topic;

		public string SerialisationTypeID
		{
			get { return "Configuration"; }
		}

		public Configuration(string name) : base()
		{
			// Set up state
			m_name = name;
			m_dirty = false;
			m_topic = MessageBus.MessageBus.Private.CreateTopic(String.Format("configuration/{0}", name));
			// Handle changes
			this.ValueChanged += OnValueChanged;
			this.ValueRemoved += OnValueRemoved;
		}

		void OnValueRemoved(IDictionary<string, string> container, string key)
		{
			m_dirty = true;
			// TODO: Publish a message indicating the change
		}

		void OnValueChanged(IDictionary<string, string> container, string key, string value)
		{
			m_dirty = true;
			// TODO: Publish a message indicating the change
		}

		public IReadOnlyDictionary<string, object> Pack()
		{
			return (IReadOnlyDictionary<string, object>)this;
		}

	}
}
