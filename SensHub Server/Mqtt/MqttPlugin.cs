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
	public class MqttPlugin : IPlugin, IConfigurable, ISubscriber, IEnableLogger
	{
		// Constants
		private const string ServerKey = "server";
		private const string IdentityKey = "identity";
		private const string TopicKey = "topic";

		// Statics
		private static Guid m_uuid = Guid.Parse("{FB992437-07CF-45FA-BD07-7671E869C6BB}");
		private static Version m_version = new Version(0, 1);

		// Instance variables
		private MqttClient m_client;
		private string m_server;
		private string m_mqttTopic;
		private string m_identity;
		private object m_lock;

		#region Properties
		public Guid UUID
		{
			get { return m_uuid; }
		}

		public Version Version
		{
			get { return m_version; }
		}
		#endregion

		public MqttPlugin()
		{
			m_lock = new Object();
		}

		#region Implementation of IPlugin
		public bool Initialise(IPluginHost host)
		{
			// Subscribe ourselves to the Public topic
			host.Subscribe(host.Public, this);
			// Remainder of initialisation done when configuration is applied.
			return true;
		}

		public void Shutdown()
		{
			// TODO: Implement this
		}
		#endregion

		#region Implementation of IConfigurable
		public void ApplyConfiguration(Configuration configuration)
		{
			// Make sure we have an identity string
			if (configuration[IdentityKey] == "")
			{
				configuration[IdentityKey] = Guid.NewGuid().ToString();
				configuration.Save();
			}
			// Reconfigure ourselves
			lock (m_lock)
			{
				if (m_client != null)
				{
					// Detach events
					m_client.ConnectionClosed -= OnMqttConnectionClosed;
					m_client.MqttMsgPublishReceived -= OnMqttMessageReceived;
					// Unsubscribe and disconnect
					m_client.Unsubscribe(new string[] { m_mqttTopic });
					m_client.Disconnect();
					// Discard the client
					m_client = null;
				}
				// Get our configuration information
				m_server = configuration[ServerKey].ToString();
				m_mqttTopic = configuration[TopicKey].ToString();
				m_identity = configuration[IdentityKey].ToString();
				// Establish the connection
				m_client = new MqttClient(m_server);
				m_client.ConnectionClosed += OnMqttConnectionClosed;
				m_client.MqttMsgPublishReceived += OnMqttMessageReceived;
				Connect();
			}
			// TODO: Implement this
		}

		private void Connect()
		{
			Task.Factory.StartNew(() =>
				{
					while (true)
					{
						lock (m_lock)
						{
							if (m_client != null)
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
									// Finish our configuration
									m_client.Subscribe(new string[] { m_mqttTopic + "/#" }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE, MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
								}
							}
						}
						Thread.Sleep(30000);
					}
				}
			);
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
