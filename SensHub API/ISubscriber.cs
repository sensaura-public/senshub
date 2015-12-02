using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SensHub.Plugins
{
	/// <summary>
	/// To subscribe to topics a class must implement this interface and subscribe
	/// to the topic on the MessageBus
	/// </summary>
	public interface ISubscriber
	{
		/// <summary>
		/// Invoked when a message arrives on a topic this subscriber is attached to.
		/// </summary>
		/// <param name="topic">The topic the message was sent on</param>
		/// <param name="source">The source of the message. May be null</param>
		/// <param name="message">The message itself</param>
		void MessageReceived(ITopic topic, object source, Message message);
	}
}
