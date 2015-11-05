using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sensaura.Utilities;
using Sensaura.MessageBus;

namespace Sensaura.Configuration
{
	public class Configuration : Dictionary<string, string>, IJsonSerialisable
	{
		private static MessageBuilder s_builder = new MessageBuilder();

		private string m_name;
		private bool m_dirty;
		private Topic m_topic;

		public string SerialisationTypeID
		{
			get { return "Configuration"; }
		}

		public Configuration(string name)
		{
			m_name = name;
			m_dirty = false;
			m_topic = MessageBus.MessageBus.Private.CreateTopic(String.Format("configuration/{0}", name));
		}

		public string Serialise()
		{
			throw new NotImplementedException();
		}

		public override void Add(string key, string value) 
		{
			base.Add(key, value);
			m_dirty = true;
			lock (s_builder)
			{
				s_builder.Add(key, value);
				m_topic.Publish(s_builder.CreateMessage());
			}
		}
	}
}
