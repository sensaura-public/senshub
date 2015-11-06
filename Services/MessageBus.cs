using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Splat;

namespace Sensaura.Services
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
		/// The root of the private topic tree. Messages in this tree should
		/// never leave the server.
		/// </summary>
		public static Topic Private
		{
			get { return m_instance.CreateTopic("private"); }
		}

		/// <summary>
		/// The root of the public topic tree. Messages in this tree may be
		/// sent outside the server.
		/// </summary>
		public static Topic Public
		{
			get { return m_instance.CreateTopic("public");  }
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
