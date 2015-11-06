using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sensaura.Utilities;
using Splat;

namespace Sensaura.Services
{
	public class Configuration : BaseDictionary<string, string>, IPackable
	{
		private static MessageBuilder s_builder = new MessageBuilder();
		private static Dictionary<string, Configuration> s_configs = new Dictionary<string, Configuration>();

		private string m_name;
		private bool m_dirty;
		private Topic m_topic;

		private Configuration(string name) : base()
		{
			// Set up state
			m_name = name;
			m_dirty = false;
			m_topic = MessageBus.Private.CreateTopic(String.Format("configuration/{0}", name));
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

		private static string GetBackingFileName(string name)
		{
			return String.Format("{0}.json", name);
		}

		public void Save()
		{

		}

		public static Configuration Open(string name)
		{
			lock (s_configs)
			{
				if (!s_configs.ContainsKey(name))
				{
					// Create a new configuration
					if (!name.IsValidIdentifier())
						throw new ArgumentException("Invalid configuration name.");
					IFileSystem fs = Locator.Current.GetService<IFileSystem>();
					IFolder folder = fs.OpenFolder("config");
					string filename = GetBackingFileName(name);
					Configuration config = null;
					if (folder.FileExists(filename))
					{
						Stream input = folder.CreateFile(
							String.Format("{0}.json", name),
							FileAccess.Read,
							CreationOptions.OpenIfExists
							);
						if (input != null)
							config = ObjectPacker.Unpack<Configuration>(input);
					}
					if (config == null)
						config = new Configuration(name);
					s_configs[name] = config;
				}
				// Configuration should now be present
				return s_configs[name];
			}
		}
	}
}
