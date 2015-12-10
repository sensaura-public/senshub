using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SensHub.Plugins;

namespace SensHub.Server.Scripting
{
	class ScriptActionFactory : AbstractActionFactory
	{
		internal static Guid MyUUID = Guid.Parse("{52F487EE-D69F-467A-AAB0-E3C2429FE6BB}");

		public override Guid UUID
		{
			get { return MyUUID; }
		}

		public override void ApplyConfiguration(Configuration configuration)
		{
			throw new NotImplementedException();
		}

		public override AbstractAction CreateInstance(Guid id, Configuration config)
		{
			ScriptAction action = new ScriptAction(id);
			action.ApplyConfiguration(config);
			return action;
		}
	}
}
