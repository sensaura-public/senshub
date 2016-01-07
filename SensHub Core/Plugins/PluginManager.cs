using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IotWeb.Common;
using SensHub.Plugins;
using Splat;

namespace SensHub.Core.Plugins
{
	/// <summary>
	/// This class manages the plugins.
	/// </summary>
	public class PluginManager : IServer, IEnableLogger
	{
		private Dictionary<Guid, AbstractPlugin> m_pluginsAvailable = new Dictionary<Guid, AbstractPlugin>();
		private Dictionary<Guid, PluginHost> m_pluginsEnabled = new Dictionary<Guid, PluginHost>();

		/// <summary>
		/// Add an individual to the master list.
		/// </summary>
		/// <param name="plugin">The plugin instance to add.</param>
		public bool AddPlugin(AbstractPlugin plugin)
		{
			lock (m_pluginsAvailable)
			{
				if (m_pluginsAvailable.ContainsKey(plugin.UUID))
				{
					this.Log().Warn("Plugin with UUID '{0}' is already registered.", plugin.UUID);
					return false;
				}
                this.Log().Debug("Adding plugin {0} (ID = '{1}')", plugin.GetType().Name, plugin.UUID);
				m_pluginsAvailable[plugin.UUID] = plugin;
			}
			return true;
		}

		/// <summary>
		/// Initialise all the plugins.
		/// 
		/// Every available plugin (added with <see cref="AddPlugin"/> or <see cref="LoadPlugins"/>)
		/// that is not marked as disabled will be initialised.
		/// </summary>
		public void InitialisePlugins()
		{
            MasterObjectTable mot = Locator.Current.GetService<MasterObjectTable>();
            lock (m_pluginsAvailable) 
			{
				lock(m_pluginsEnabled) 
				{
					foreach (Guid uuid in m_pluginsAvailable.Keys)
					{
						// Create the IPluginHost for this plugin and enable it
						PluginHost host = new PluginHost(m_pluginsAvailable[uuid]);
						if (host.EnablePlugin())
						{
                            this.Log().Debug("Enabled plugin {0} (ID = '{1}')", m_pluginsAvailable[uuid].GetType().Name, m_pluginsAvailable[uuid].UUID);
                            m_pluginsEnabled[uuid] = host;
                            mot.AddInstance(m_pluginsAvailable[uuid]);
						}
					}
				}
			}
		}

		/// <summary>
		/// Shutdown all the plugins
		/// </summary>
		public void ShutdownPlugins()
		{
			lock (m_pluginsEnabled)
			{
				foreach (Guid uuid in m_pluginsEnabled.Keys)
				{
					m_pluginsEnabled[uuid].ShutdownPlugin();
				}
			}
		}

		#region Implementation of IServer
		public event ServerStoppedHandler ServerStopped;

		public bool Running { get; private set; }

		public void Start()
		{
			// TODO: Implement this
		}

		public void Stop()
		{
			// TODO: Implement this
		}
		#endregion

	}
}
