using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using SensHub.Plugins;
using Splat;

namespace SensHub.Server.Mqtt
{
	public class MqttPlugin : AbstractPlugin, IConfigurable, ISubscriber, IEnableLogger
	{
		/// <summary>
		/// Possible states for the service
		/// </summary>
		private enum ServiceState
		{
			Disconnecting,
			Disconnected,
			Connecting,
			Connected
		}

		// Constants
		private const string ServerKey = "server";
		private const string IdentityKey = "identity";
		private const string TopicKey = "topic";
		private const long RetryDelay = 30000;
		private const int RetryCount = 10;

		// Statics
		private static Guid m_uuid = Guid.Parse("{FB992437-07CF-45FA-BD07-7671E869C6BB}");
		private static Version m_version = new Version(0, 1);

		// Instance variables
		private MqttClient m_client;
		private string m_server;
		private string m_mqttTopic;
		private string m_identity;
		private object m_lock;
		private ServiceState m_state;

		#region Properties
		public override Guid UUID
		{
			get { return m_uuid; }
		}

		public override Version Version
		{
			get { return m_version; }
		}
		#endregion

		public MqttPlugin()
		{
			m_lock = new Object();
			m_state = ServiceState.Disconnected;
		}

		#region Service Implementation
		/// <summary>
		/// Disconnect from the server and clean up state
		/// </summary>
		private void CleanUpConnection()
		{
			lock (m_lock)
			{
				m_state = ServiceState.Disconnecting;
				m_client.ConnectionClosed -= OnMqttConnectionClosed;
				m_client.MqttMsgPublishReceived -= OnMqttMessageReceived;
				// Unsubscribe and disconnect
				m_client.Unsubscribe(new string[] { m_mqttTopic });
				if (m_client.IsConnected)
					m_client.Disconnect();
				m_client = null;
				m_state = ServiceState.Disconnected;
			}
		}

		/// <summary>
		/// Background task to establish a connection to the server.
		/// </summary>
		/// <param name="state"></param>
		private void ConnectionCallback(object state)
		{
			lock (m_lock)
			{
				// Create the client and attach events if needed
				if (m_state != ServiceState.Disconnected)
				{
					m_client = new MqttClient(m_server);
					m_client.ConnectionClosed += OnMqttConnectionClosed;
					m_client.MqttMsgPublishReceived += OnMqttMessageReceived;
				}
			}
			DateTime lastAttempt = DateTime.Now;
			int attempts = 0;
			while ((m_state == ServiceState.Connecting) && (attempts < RetryCount))
			{
				// Is it time for a new connection attempt ?
				if ((attempts > 0) && ((DateTime.Now - lastAttempt).TotalMilliseconds < RetryDelay))
				{
					Thread.Sleep(10);
					continue;
				}
				// Try connecting again
				lastAttempt = DateTime.Now;
				attempts++;
				lock (m_lock)
				{
					byte result = 0xff;
					try
					{
						result = m_client.Connect(m_identity);
					}
					catch (Exception ex)
					{
						this.Log().Warn("Failed to connect to MQTT server at '{0}' - {1}", m_server, ex.Message);
					}
					if (result == 0)
					{
						string topic = m_mqttTopic + "/#";
						this.Log().Debug("Connection established, subscribing to '{0}'", topic);
						try
						{
							m_client.Subscribe(new string[] { topic }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE });
							m_state = ServiceState.Connected;
						}
						catch (Exception ex)
						{
							this.Log().Warn("Subscribption failed to topic '{0}' on server at '{1}'", topic, m_server);
						}
					}
				}
			}
			if (m_state != ServiceState.Connected)
			{
				if (m_state != ServiceState.Disconnecting)
					this.Log().Error("Unabled to connect to MQTT server at {0} after {1} attempts. Please check your configuration.", m_server, attempts);
				CleanUpConnection();
			}
		}

		/// <summary>
		/// State transition management
		/// </summary>
		/// <param name="state"></param>
		private void SetState(ServiceState state)
		{
			if (state == m_state)
				return;
			switch (state)
			{
				case ServiceState.Connecting:
					if (m_state != ServiceState.Disconnected)
					{
						this.Log().Warn("Invalid state - cannot enter 'Connecting' from '{0}'.", m_state.ToString());
						return;
					}
					// Start a background thread to establish the connection.
					m_state = ServiceState.Connecting;
					ThreadPool.QueueUserWorkItem(ConnectionCallback);
					break;
				case ServiceState.Disconnected:
					if (m_state == ServiceState.Connecting)
						m_state = ServiceState.Disconnecting;
					else
						CleanUpConnection();
					// Wait until we get to Disconnected
					while (m_state != ServiceState.Disconnected)
						Thread.Sleep(10);
					break;
			}
		}
		#endregion

		#region Implementation of AbstractPlugin
		public override bool Initialise(IPluginHost host)
		{
			// Subscribe ourselves to the Public topic
			host.Subscribe(host.Public, this);
			// Remainder of initialisation done when configuration is applied.
			return true;
		}

		public override void Shutdown()
		{
			SetState(ServiceState.Disconnected);
		}
		#endregion

		#region Implementation of IConfigurable
		public void ApplyConfiguration(Configuration configuration)
		{
			// Make sure we have an identity string
			if (configuration[IdentityKey].ToString() == "")
			{
				configuration[IdentityKey] = Guid.NewGuid().ToString();
				configuration.Save();
			}
			// Make sure we are in the disconnected state
			SetState(ServiceState.Disconnected);
			// Get our configuration information
			m_server = configuration[ServerKey].ToString();
			m_mqttTopic = configuration[TopicKey].ToString();
			m_identity = configuration[IdentityKey].ToString();
			this.Log().Debug("MQTT Connection Configuration - server = '{0}', mqttTopic = '{1}', identity = '{2}'",
				m_server,
				m_mqttTopic,
				m_identity
				);
			// Start connecting
			SetState(ServiceState.Connecting);
		}

		void OnMqttMessageReceived(object sender, uPLibrary.Networking.M2Mqtt.Messages.MqttMsgPublishEventArgs e)
		{
			// TODO: Implement this
			this.Log().Debug("MQTT Message Received");
		}

		void OnMqttConnectionClosed(object sender, EventArgs e)
		{
			// TODO: Implement this
			this.Log().Warn("MQTT Connection lost.");
		}
		#endregion

		#region Implementation of ISubscriber
		public void MessageReceived(ITopic topic, object source, Message message)
		{
			throw new NotImplementedException();
		}
		#endregion

	}
}
