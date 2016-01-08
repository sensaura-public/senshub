using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IotWeb.Common.Http;
using SensHub.Plugins;
using SensHub.Core.Plugins;
using SensHub.Core.Utils;
using Splat;

namespace SensHub.Core.Http
{
	class RpcConnection : ISubscriber
	{
		/// <summary>
		/// Supported RPC functions
		/// </summary>
		enum RpcFunction
		{
			Authenticate,
			Subscribe,
			Unsubscribe,
			GetState,
			GetConfiguration,
			SetConfiguration,
			CreateInstance
		}

		// Expected fields in incoming requests
		private const string FunctionName = "function";
		private const string FunctionSequence = "sequence";
		private const string FunctionParameters = "params";
		private const string CallStatus = "status";
		private const string CallResult = "result";
		private const string MessageTopic = "topic";
		private const string MessagePayload = "payload";

		// Instance variables
		private WebSocket m_socket;
		private bool m_authenticated;
		private IMessageBus m_messagebus;
		private MasterObjectTable m_mot;
		private MessageBuilder m_builder;
		
		/// <summary>
		/// Constructor with a socket
		/// </summary>
		/// <param name="socket"></param>
		public RpcConnection(WebSocket socket)
		{
			m_socket = socket;
			m_authenticated = false;
			// Get references to the core components we need
			m_builder = new MessageBuilder();
			m_messagebus = Locator.Current.GetService<IMessageBus>();
			m_mot = Locator.Current.GetService<MasterObjectTable>();
			// Listen for incoming messages
			m_socket.DataReceived += OnDataReceived;
		}

		/// <summary>
		/// Invoked when a new frame is recieved from the client
		/// </summary>
		/// <param name="socket"></param>
		/// <param name="frame"></param>
		void OnDataReceived(WebSocket socket, string frame)
		{
			// Decode the frame
			IDictionary<string, object> data = ObjectPacker.UnpackRaw(frame);
			if (data == null)
				return;
			// Is it a message ?
			if (ContainsKeys(data, MessageTopic, MessagePayload))
				ProcessMessage(data);
			else if (ContainsKeys(data, FunctionSequence, FunctionName, FunctionParameters))
				ProcessRpcCall(data);
		}

		/// <summary>
		/// Called when a new message is sent to a topic we subscribe to
		/// </summary>
		/// <param name="topic"></param>
		/// <param name="source"></param>
		/// <param name="message"></param>
		public void MessageReceived(ITopic topic, object source, Message message)
		{
			Dictionary<string, object> frameData = new Dictionary<string, object>();
			frameData[MessageTopic] = topic.ToString();
			frameData[MessagePayload] = message;
			string frame = JsonParser.ToJson(frameData);
			lock (m_socket)
			{
				m_socket.Send(frame);
			}
		}

		/// <summary>
		/// Close the connection and clean up resources
		/// </summary>
		public void Close() 
		{
			// Unsubscribe from all topics
			m_messagebus.Unsubscribe(this);
			// Close the connection
			m_socket.Close();
		}

		#region Helper Methods
		/// <summary>
		/// Ensure the dictionay contains the specified keys.
		/// </summary>
		/// <param name="values"></param>
		/// <param name="keys"></param>
		/// <returns></returns>
		private bool ContainsKeys(IDictionary<string, object> values, params string[] keys)
		{
			foreach (string key in keys)
				if (!values.ContainsKey(key))
					return false;
			return true;
		}

		/// <summary>
		/// Publish a message received over the connection
		/// 
		/// If the connection is subscribing to the topic the message is published
		/// to it will receive the message back.
		/// </summary>
		/// <param name="data"></param>
		private void ProcessMessage(IDictionary<string, object> data)
		{
			// Must be authenticated to send messages
			if (!m_authenticated)
				return;
			// Make sure the data is valid
			ITopic topic = m_messagebus.Create(data[MessageTopic].ToString());
			if (topic == null)
				return;
			// Create the message
			IDictionary<string, object> message = data[MessagePayload] as IDictionary<string, object>;
			if (message == null)
				return;
			// Convert to a message instance and send
			lock (m_builder)
			{
				m_builder.Clear();
				foreach (string key in message.Keys)
					m_builder.Add(key, message[key]);
				m_messagebus.Publish(topic, m_builder.CreateMessage());
			}
		}

		/// <summary>
		/// Process an RPC call
		/// 
		/// This implementation is very manual but is probably enough for the very
		/// small set of API calls we support.
		/// </summary>
		/// <param name="data"></param>
		private void ProcessRpcCall(IDictionary<string, object> data)
		{
			RpcFunction function;
			Dictionary<string, object> result = new Dictionary<string,object>();
			result[FunctionSequence] = data[FunctionSequence];
			if (!Enum.TryParse<RpcFunction>(data[FunctionName].ToString(), out function))
			{
				result[CallStatus] = false;
				result[CallResult] = String.Format("Unsupported function '{0}'", data[FunctionName].ToString());
			}
			else
			{
				// Verify the arguments are at least a dictionary
				IDictionary<string, object> args = data[FunctionParameters] as IDictionary<string, object>;
				if (args == null)
				{
					result[CallStatus] = false;
					result[CallResult] = "Invalid arguments provided.";
				}
				else
				{
					try
					{
						object functionResult = null;
						switch (function)
						{
							case RpcFunction.Authenticate:
								functionResult = RpcAuthenticate(args);
								break;
							case RpcFunction.Subscribe:
								functionResult = RpcSubscribe(args);
								break;
							case RpcFunction.Unsubscribe:
								functionResult = RpcUnsubscribe(args);
								break;
							case RpcFunction.GetState:
								functionResult = RpcGetState(args);
								break;
							case RpcFunction.GetConfiguration:
								functionResult = RpcGetConfiguration(args);
								break;
							case RpcFunction.SetConfiguration:
								functionResult = RpcSetConfiguration(args);
								break;
							case RpcFunction.CreateInstance:
								functionResult = RpcCreateInstance(args);
								break;
						}
						result[CallStatus] = true;
						result[CallResult] = functionResult;
					}
					catch (Exception ex)
					{
						result[CallStatus] = false;
						result[CallResult] = ex.Message;
					}
				}
			}
			// Finally we can send back the response
			string json = JsonParser.ToJson(result);
			lock (m_socket)
			{
				m_socket.Send(json);
			}
		}
		#endregion

		#region RPC Functions
		private object RpcAuthenticate(IDictionary<string, object> args)
		{
			return null;
		}

		private object RpcSubscribe(IDictionary<string, object> args)
		{
			return null;
		}

		private object RpcUnsubscribe(IDictionary<string, object> args)
		{
			return null;
		}

		private object RpcGetState(IDictionary<string, object> args)
		{
			return null;
		}

		private object RpcGetConfiguration(IDictionary<string, object> args)
		{
			return null;
		}

		private object RpcSetConfiguration(IDictionary<string, object> args)
		{
			return null;
		}

		private object RpcCreateInstance(IDictionary<string, object> args)
		{
			return null;
		}
		#endregion
	}
}
