using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SensHub.Plugins;
using Splat;

namespace SensHub.Server.Mqtt
{
	public class MessageBus : Topic, IMessageBus, IEnableLogger
	{
		/// <summary>
		/// Holds all the information needed to process a message.
		/// </summary>
		private struct QueuedMessage
		{
			public ITopic Topic;
			public ISet<ISubscriber> Subscribers;
			public object Source;
			public Message Payload;
		}

		/// <summary>
		/// A blocking queue for processing messages in a linear fashion.
		/// </summary>
		private class MessageQueue : BlockingCollection<QueuedMessage>
		{
			/// <summary>
			/// Initializes a new instance of the ProducerConsumerQueue, Use Add and TryAdd for Enqueue and TryEnqueue and Take and TryTake for Dequeue and TryDequeue functionality
			/// </summary>
			public MessageQueue()
				: base(new ConcurrentQueue<QueuedMessage>())
			{
			}

		}

		// Instance variables
		private MessageQueue m_queue;
		private Dictionary<ITopic, ISet<ISubscriber>> m_subscribers;
		private Dictionary<ISubscriber, ISet<ITopic>> m_topics;
		private object m_lock;

		public MessageBus()
			: base(null, "")
		{
			// Set up root topics
			Public = Create("public");
			Private = Create("private");
			// Set up the queue
			m_queue = new MessageQueue();
			// Set up subscriber mappings
			m_lock = new Object();
			m_subscribers = new Dictionary<ITopic, ISet<ISubscriber>>();
			m_topics = new Dictionary<ISubscriber, ISet<ITopic>>();
		}

		/// <summary>
		/// Run the messagebus
		/// 
		/// This is a blocking method, it will run in a loop until the
		/// server is requested to shut down.
		/// </summary>
		public void Run()
		{
			while (true)
			{
				QueuedMessage message = m_queue.Take();
				// Dispatch to all subscribers
				foreach (ISubscriber subscriber in message.Subscribers)
				{
					try
					{
						subscriber.MessageReceived(message.Topic, message.Source, message.Payload);
					}
					catch (Exception ex)
					{
						this.Log().Error("Failed to dispatch message to subscriber - {0}", ex.ToString());
					}
				}
				// TODO: Check for server shutdown message
				if (System.Console.KeyAvailable)
					return;
			}
		}

		#region Internal Implementation
		/// <summary>
		/// Get a list of all subscribers attached to a given topic.
		/// 
		/// This include subscribers on all parent topics as well.
		/// </summary>
		/// <param name="topic"></param>
		/// <returns></returns>
		private ISet<ISubscriber> GetSubscribersForTopic(ITopic topic)
		{
			ISet<ISubscriber> results = new HashSet<ISubscriber>();
			lock (m_lock)
			{
				while (topic != null)
				{
					if (m_subscribers.ContainsKey(topic))
					{
						foreach (ISubscriber subscriber in m_subscribers[topic])
							results.Add(subscriber);
					}
					topic = topic.Parent;
				}
			}
			return results;
		}

		/// <summary>
		/// Get the set of all topics this subscriber is attached to.
		/// </summary>
		/// <param name="subscriber"></param>
		/// <returns></returns>
		private ISet<ITopic> GetTopicsForSubscriber(ISubscriber subscriber)
		{
			ISet<ITopic> results = new HashSet<ITopic>();
			lock (m_lock)
			{
				if (m_topics.ContainsKey(subscriber))
				{
					foreach (ITopic topic in m_topics[subscriber])
						results.Add(topic);
				}
			}
			return results;
		}
		#endregion

		#region Implementation of IMessageBus
		public ITopic Public { get; private set; }

		public ITopic Private { get; private set; }

		public void Subscribe(ITopic topic, ISubscriber subscriber)
		{
			lock (m_lock)
			{
				// Attach the subscriber to the topic
				if (!m_subscribers.ContainsKey(topic))
					m_subscribers[topic] = new HashSet<ISubscriber>();
				m_subscribers[topic].Add(subscriber);
				// Attach the topic to the subscriber
				if (!m_topics.ContainsKey(subscriber))
					m_topics[subscriber] = new HashSet<ITopic>();
				m_topics[subscriber].Add(topic);
			}
		}

		public void Unsubscribe(ITopic topic, ISubscriber subscriber)
		{
			lock (m_lock)
			{
				// Remove the subscriber from the topic
				if (m_subscribers.ContainsKey(topic))
					m_subscribers[topic].Remove(subscriber);
				// Remove the topic from the subscriber
				if (m_topics.ContainsKey(subscriber))
				{
					m_topics[subscriber].Remove(topic);
					if (m_topics[subscriber].Count == 0)
					{
						// Remove the subscriber as a key so it can be garbage collected
						m_topics.Remove(subscriber);
					}
				}
			}
		}

		public void Unsubscribe(ISubscriber subscriber)
		{
			foreach (ITopic topic in GetTopicsForSubscriber(subscriber))
				Unsubscribe(topic, subscriber);
		}

		public void Publish(ITopic topic, Message message, object source = null)
		{
			ISet<ISubscriber> subscribers = GetSubscribersForTopic(topic);
			if (subscribers.Count == 0)
				return; // No one is interested
			// Add it to the queue for later processing
			m_queue.Add(new QueuedMessage()
				{
					Topic = topic,
					Payload = message,
					Source = source,
					Subscribers = subscribers
				});
		}
		#endregion
	}
}
