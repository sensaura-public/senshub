using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SensHub.Plugins
{
	public interface IMessageBus
	{
		/// <summary>
		/// The root of the 'public' topic tree. Messages sent to this
		/// tree are replicated on the MQTT server.
		/// </summary>
		ITopic Public { get; }

		/// <summary>
		/// The root of the 'private' topic tree. Messages sent to this
		/// tree are not pushed outside the server.
		/// </summary>
		ITopic Private { get; }

		/// <summary>
		/// Subscribe to a specific topic.
		/// 
		/// Once subscribed any messages sent to the topic (or any of it's
		/// child topics) will be passed to the subscriber instance.
		/// </summary>
		/// <param name="topic">The topic to listen to for messages</param>
		/// <param name="subscriber">The ISubscriber instance to receive messages</param>
		void Subscribe(ITopic topic, ISubscriber subscriber);

		/// <summary>
		/// Unsubscribe from a topic
		/// 
		/// If the subscriber is attached to the topic it will be removed and
		/// messages for the topic will no longer be passed to it.
		/// </summary>
		/// <param name="topic">The topic to unsubscribe from</param>
		/// <param name="subscriber">The subscriber to remove</param>
		void Unsubscribe(ITopic topic, ISubscriber subscriber);

		/// <summary>
		/// Unsubscribe from all topics
		/// 
		/// This method will remove the subscriber from all topics it is
		/// attached to.
		/// </summary>
		/// <param name="subscriber">The subscriber to remove</param>
		void Unsubscribe(ISubscriber subscriber);

		/// <summary>
		/// Publish a message to the given topic.
		/// 
		/// When a message is published it is passed to the specified
		/// topic and all parent topics. Subscribers attached to any
		/// of those topics will be notified that the message has
		/// arrived.
		/// 
		/// You may optionally provide a source for the message. If
		/// the source is an ISubscriber instance that would be
		/// triggered by the message the triggering will not occur,
		/// allowing you to ignore messages you send yourself.
		/// </summary>
		/// <param name="topic">The topic to publish to</param>
		/// <param name="message">The message to publish.</param>
		/// <param name="source">The (optional) source of the message.</param>
		void Publish(ITopic topic, Message message, object source = null);
	}
}
