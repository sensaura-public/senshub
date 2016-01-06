using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SensHub.Plugins;
using Splat;

namespace SensHub.Core.Messages
{
	public class MessageBus : Topic, IMessageBus, IEnableLogger
	{
		/// <summary>
		/// Holds all the information needed to process a message.
		/// </summary>
		private class QueuedMessage
		{
			public ITopic Topic { get; private set; }
			public ISet<ISubscriber> Subscribers { get; private set; }
            public object Source { get; private set; }
            public Message Payload { get; private set; }

            public QueuedMessage(ITopic topic, ISet<ISubscriber> subscribers, object source, Message payload)
            {
                Topic = topic;
                Subscribers = subscribers;
                Source = source;
                Payload = payload;
            }
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

		// Constants
		private const int QueueWaitPeriod = 1000;
		private const int HeartBeatPeriod = 60000;

		// Instance variables
		private MessageQueue m_queue;
		private Dictionary<ITopic, ISet<ISubscriber>> m_subscribers;
		private Dictionary<ISubscriber, ISet<ITopic>> m_topics;
		private object m_lock;
		private int m_messagesProcessed;
		private int m_messagesReceived;
		private DateTime m_lastHeartbeat;
		private MessageBuilder m_builder;

		public MessageBus()
			: base(null, "")
		{
			// Set up root topics
			Public = Create("public");
			Private = Create("private");
			// Set up the queue
			m_queue = new MessageQueue();
			m_builder = new MessageBuilder();
			m_lastHeartbeat = DateTime.Now;
			m_messagesProcessed = 0;
			m_messagesReceived = 0;
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
			QueuedMessage message;
			while (true)
			{
				if (m_queue.TryTake(out message, QueueWaitPeriod))
				{
					// Dispatch to all subscribers
					foreach (ISubscriber subscriber in message.Subscribers)
					{
						if (subscriber != message.Source)
						{
							Task.Factory.StartNew(() => 
							{
								try
								{
									subscriber.MessageReceived(message.Topic, message.Source, message.Payload);
									m_messagesProcessed++;
								}
								catch (Exception ex)
								{
									this.Log().Error("Failed to dispatch message to subscriber - {0}", ex.ToString());
								}
							});
						}
					}
				}
				// Do we need to send a heartbeat message ?
				if ((DateTime.Now - m_lastHeartbeat).TotalMilliseconds >= HeartBeatPeriod)
				{
					m_builder.Add("messagesReceived", m_messagesReceived);
					m_builder.Add("messagesHandled", m_messagesProcessed);
					Publish(Create(Topics.ServerHeartbeat), m_builder.CreateMessage());
					this.Log().Info("Received {0} messages, {1} dispatched to subscribers.", m_messagesReceived, m_messagesProcessed);
					m_messagesProcessed = 0;
					m_messagesReceived = 0;
					m_lastHeartbeat = DateTime.Now;
				}
/*
				// TODO: Check for server shutdown message
				if (System.Console.KeyAvailable)
				{
					System.Console.ReadKey();
					return;
				}
*/
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
            if (topic == null)
                throw new ArgumentNullException();
			m_messagesReceived++;
			// Get the set of subscribers
			ISet<ISubscriber> subscribers = GetSubscribersForTopic(topic);
			if (subscribers.Count == 0)
				return; // No one is interested
            // Add it to the queue for later processing
            m_queue.Add(new QueuedMessage(topic, subscribers, source, message));
		}
		#endregion
	}
}
