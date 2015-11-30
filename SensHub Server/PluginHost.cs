using System;
using System.Reflection;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SensHub.Plugins;
using Splat;

namespace SensHub.Server
{
	/// <summary>
	/// Implements the <see cref="IPluginHost"/> interface for plugins.
	/// 
	/// Each plugin is given it's own instance of a host, this restricts
	/// file system and configuration access to the plugins domain.
	/// </summary>
	internal class PluginHost : IPluginHost, IEnableLogger
	{
		
		private IPlugin m_plugin;
		private IFolder m_data;

		public PluginHost(IPlugin plugin)
		{
			m_plugin = plugin;
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

		public IFolder FileSystem
		{
			get { return m_data; }
		}

		#endregion

		#region Custom Operations

		public bool EnablePlugin()
		{
			bool initialised = false;
			// Set up the data directory for the plugin
			FileSystem fs = Locator.Current.GetService<FileSystem>();
			fs = fs.OpenFolder("data") as FileSystem;
			try
			{
				m_data = fs.OpenFolder(m_plugin.UUID.ToString());
			}
			catch (Exception ex)
			{
				this.Log().Error("Unable to create data directory for plugin {0}", m_plugin.UUID.ToString());
				return false;
			}
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
				this.Log().Error("Failed to initialise plugin - {0}", ex.ToString());
			}
			return initialised;
		}
		#endregion

	}
}
