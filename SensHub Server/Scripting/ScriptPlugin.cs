using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SensHub.Plugins;

namespace SensHub.Server.Scripting
{
	public class ScriptPlugin : AbstractPlugin
	{
		private static Guid MyUUID = Guid.Parse("{DEF762E7-9BB1-4F55-AF14-D1B25F2F9198}");
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
            //return host.RegisterActionFactory(new ScriptActionFactory());
            return true;
		}

		public override void Shutdown()
		{
			// TODO: Implement this
		}
	}
}
