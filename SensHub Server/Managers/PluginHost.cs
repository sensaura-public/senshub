using System;
using System.Reflection;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SensHub.Server;
using SensHub.Plugins;
using Splat;

namespace SensHub.Server.Managers
{
	/// <summary>
	/// Implements the <see cref="IPluginHost"/> interface for plugins.
	/// 
	/// Each plugin is given it's own instance of a host, this restricts
	/// file system and configuration access to the plugins domain.
	/// </summary>
	internal class PluginHost : IPluginHost, IEnableLogger
	{
		
		private AbstractPlugin m_plugin;
		private IFolder m_data;
		private IMessageBus m_messageBus;

		public PluginHost(AbstractPlugin plugin)
		{
			m_plugin = plugin;
			m_messageBus = Locator.Current.GetService<IMessageBus>();
		}

		#region Implementation of IPluginHost
		public Version Version
		{
			get { return Assembly.GetEntryAssembly().GetName().Version; }
		}

		public CultureInfo Culture
		{
			get { return CultureInfo.CurrentCulture; }
		}

		public IFolder Datastore
		{
			get 
			{
				lock (this)
				{
					if (m_data == null)
					{
						FileSystem fs = Locator.Current.GetService<FileSystem>();
						m_data = fs.OpenFolder(FileSystem.DataFolder).OpenFolder(m_plugin.UUID.ToString());
					}
				}
				return m_data; 
			}
		}

		public ITopic Parent
		{
			get { return m_messageBus.Parent; }
		}

		public ITopic Public
		{
			get { return m_messageBus.Public; }
		}

		public ITopic Private
		{
			get { return m_messageBus.Private; }
		}

		public ITopic Create(string topic)
		{
			return m_messageBus.Create(topic);
		}

		public void Subscribe(ITopic topic, ISubscriber subscriber)
		{
			m_messageBus.Subscribe(topic, subscriber);
		}

		public void Unsubscribe(ITopic topic, ISubscriber subscriber)
		{
			m_messageBus.Unsubscribe(topic, subscriber);
		}

		public void Unsubscribe(ISubscriber subscriber)
		{
			m_messageBus.Unsubscribe(subscriber);
		}

		public void Publish(ITopic topic, Message message, object source = null)
		{
			m_messageBus.Publish(topic, message, source);
		}

		public bool RegisterActionFactory(AbstractActionFactory factory)
		{
			// TODO: Implement this
			return false;
		}

		public bool RegisterSourceFactory(AbstractSourceFactory factory)
		{
			// TODO: Implement this
			return false;
		}
		#endregion

		#region Custom Operations
		/// <summary>
		/// Enable the plugin
		/// </summary>
		/// <returns></returns>
		public bool EnablePlugin()
		{
			// Add the plugin to the MOT on the assumption it will work
			MasterObjectTable mot = Locator.Current.GetService<MasterObjectTable>();
			mot.AddInstance(m_plugin);
			// If the plugin supports configuration we need to give it one before
			// initialisation
			IConfigurable configurable = m_plugin as IConfigurable;
			if (configurable != null)
			{
				IConfigurationDescription description = mot.GetConfigurationDescription(m_plugin.UUID);
				IDictionary<string, object> values = mot.GetConfiguration(m_plugin.UUID);
				Dictionary<string, string> failures = new Dictionary<string,string>();
				if (!configurable.ValidateConfiguration(description, values, failures))
				{
					StringBuilder sb = new StringBuilder();
					string separator = "";
					foreach (KeyValuePair<string, string> message in failures)
					{
						sb.Append(separator);
						separator = ", ";
						sb.Append(String.Format("'{0}' - {1}", message.Key, message.Value));
					}
					this.Log().Error("One or more configuration values are not applicable - " + sb.ToString());
					mot.RemoveInstance(m_plugin.UUID);
					return false;
				}
				try
				{
					configurable.ApplyConfiguration(description, values);
				}
				catch (Exception ex)
				{
					this.Log().Error("Unable to apply configuration to plugin - {0}.", ex.ToString());
					mot.RemoveInstance(m_plugin.UUID);
					return false;
				}
			}
			// Now try and initialise it
			bool initialised = false;
			try
			{
				initialised = m_plugin.Initialise(this);
				if (initialised)
					this.Log().Info("Initialised plugin");
				else
					this.Log().Error("Failed to initialise plugin.");
			}
			catch (Exception ex)
			{
				this.Log().Error("Failed to initialise plugin - {0}", ex.Message);
			}
			if (!initialised)
			{
				mot.RemoveInstance(m_plugin.UUID);
			}
			return initialised;
		}

		/// <summary>
		/// Shutdown the plugin
		/// </summary>
		public void ShutdownPlugin()
		{
			try
			{
				this.Log().Debug("Shutting down plugin {0}", m_plugin.GetType().Name);
				m_plugin.Shutdown();
			}
			catch (Exception ex)
			{
				this.Log().Error("Failed to shutdown plugin {0} - {1}", m_plugin.GetType().Name, ex.Message);
			}
		}
		#endregion

	}
}
