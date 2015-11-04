using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Splat;

namespace Sensaura.MessageBus
{
	public class MessageBus : Topic, IEnableLogger
	{
		//--- Static variables
		private static readonly MessageBus m_instance;

		/// <summary>
		/// Singleton instance property
		/// </summary>
		public static MessageBus Instance
		{
			get { return m_instance; }
		}

		/// <summary>
		/// Static initialisation
		/// </summary>
		static MessageBus()
		{
			m_instance = new MessageBus();
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		private MessageBus()
			: base(null, "")
		{

		}
	}
}
