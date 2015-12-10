using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SensHub.Plugins;

namespace SensHub.Server.Http
{
	public class WebHookPlugin : AbstractPlugin
	{
		//-- Identification
		private static Guid MyUUID = Guid.Parse("{B5BD4E18-C3AE-4C76-9530-530E2DA668C7}");
		private static Version MyVersion = new Version(0, 1);

		public override Guid UUID
		{
			get { return MyUUID; }
		}

		public override Version Version
		{
			get { return MyVersion; }
		}

		public override bool Initialise(IPluginHost host)
		{
			// TODO: Implement this
			return true;
		}

		public override void Shutdown()
		{
			// TODO: Implement this
		}
	}
}
