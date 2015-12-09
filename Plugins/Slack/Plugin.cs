using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SensHub.Plugins;
using Splat;

namespace SensHub.Extension.Slack
{
    public class Plugin : AbstractPlugin, IEnableLogger
    {
		// {C7E8BB44-B05A-4BC6-937D-F0E15001BC57}
		private static Guid PluginUUID = Guid.Parse("{C7E8BB44-B05A-4BC6-937D-F0E15001BC57}"); 

		// Plugin version
		private static Version PluginVersion = new Version(0, 1);

		/// <summary>
		/// Make the host available to the rest of the implementation.
		/// </summary>
		public IPluginHost Host { get; private set; }

		public override Guid UUID
		{
			get { return PluginUUID; }
		}

		public override Version Version
		{
			get { return PluginVersion; }
		}

		public override bool Initialise(IPluginHost host)
		{
			// TODO: Implement this correctly
			Host = host;
			this.Log().Debug("Initialised plugin. Plugin version = {0}, Host version = {1}", Version, Host.Version);
			return true;
		}

		public override void Shutdown()
		{
			// TODO: Implement this
		}

	}
}
