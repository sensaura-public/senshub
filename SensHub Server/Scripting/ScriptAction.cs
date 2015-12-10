using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SensHub.Plugins;

namespace SensHub.Server.Scripting
{
	public class ScriptAction : AbstractAction
	{
		//--- Instance variables
		private Guid m_uuid;

		public ScriptAction(Guid uuid)
		{
			m_uuid = uuid;
		}

		public override Guid ParentUUID
		{
			get { return ScriptActionFactory.MyUUID; }
		}

		public override Guid UUID
		{
			get { return m_uuid; }
		}

		public override void ApplyConfiguration(Configuration configuration)
		{
			throw new NotImplementedException();
		}

		public override void Invoke(Message message)
		{
			throw new NotImplementedException();
		}
	}
}
