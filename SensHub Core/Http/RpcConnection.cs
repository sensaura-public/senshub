using System;
using System.Reflection;
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
    [AttributeUsage(AttributeTargets.Method)]
    class RpcExport : Attribute
    {
        /// <summary>
        /// The public name of function
        /// </summary>
        public readonly string FunctionName;

        /// <summary>
        /// If authentication is required to invoke the method.
        /// </summary>
        public bool AuthenticationRequired { get; set; }

        /// <summary>
        /// Constructor, does nothing - use named parameters
        /// </summary>
        public RpcExport(string functionName)
        {
            FunctionName = functionName;
            AuthenticationRequired = false;
        }
    }

	class RpcMethod 
	{
		public MethodInfo Method { get; set; }
		public RpcExport Exported { get; set; }
		public string[] Arguments { get; set; }
	}

	class RpcConnection : ISubscriber
	{
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
		
		#region RPC Method Cache
		private static Dictionary<string, RpcMethod> s_functionCache = new Dictionary<string, RpcMethod>();

		/// <summary>
		/// Static constructor
		/// 
		/// Initialise the available RPC methods by inspecting the class
		/// </summary>
		static RpcConnection()
		{
			// Find all methods on the instance decorated with the RpcCall attribute
            foreach (MethodInfo method in typeof(RpcConnection).GetRuntimeMethods())
            {
				// Is the method marked for RPC export ?
				RpcExport exported = null;
				foreach (object attribute in method.GetCustomAttributes(true))
					if (attribute is RpcExport)
					{
						exported = (RpcExport)attribute;
						break;
					}
                if (exported == null)
                    continue;
				// Build up information about the call
				RpcMethod methodInfo = new RpcMethod();
				methodInfo.Method = method;
				methodInfo.Exported = exported;
				ParameterInfo[] args = method.GetParameters();
				methodInfo.Arguments = new string[args.Length];
				for (int i = 0; i < args.Length; i++)
					methodInfo.Arguments[i] = args[i].Name;
				// Add it to the cache (TODO: Should check for duplicates)
				s_functionCache.Add(methodInfo.Exported.FunctionName, methodInfo);
			}
		}
		#endregion

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
			Dictionary<string, object> result = new Dictionary<string,object>();
			result[FunctionSequence] = data[FunctionSequence];
			try 
			{
				// Is it a valid function name ?
				if (!s_functionCache.ContainsKey(data[FunctionName].ToString())) 
					throw new RpcException(String.Format("Unsupported or unrecognised function '{0}'", data[FunctionName]));
				// Verify the arguments are at least a dictionary
				IDictionary<string, object> args = data[FunctionParameters] as IDictionary<string, object>;
				if (args == null)
					throw new RpcException("Incorrectly formed RPC request");
				// Are all the required arguments present ?
				RpcMethod method = s_functionCache[data[FunctionName].ToString()];
				if (!ContainsKeys(args, method.Arguments))
					throw new RpcException("One or more required arguments are missing.");
				if (method.Arguments.Length != args.Count)
					throw new RpcException("Unexpected additional arguments were provided.");
				// Is authentication required ?
				if (method.Exported.AuthenticationRequired && !m_authenticated)
					throw new RpcException("Authentication required");
				// Map named parameters to an array for invokation
				object[] callParams = new object[method.Arguments.Length];
				for (int i = 0; i<method.Arguments.Length; i++)
					callParams[i] = args[method.Arguments[i]];
				// Finally, call the method
				result[CallResult] = method.Method.Invoke(this, callParams);
				result[CallStatus] = true;
			}
			catch (Exception ex) 
			{
				result[CallStatus] = false;
				result[CallResult] = ex.Message;
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
		/// <summary>
		/// Authenticate the remote user
		/// </summary>
		/// <param name="password">The password to use for authentication</param>
		/// <returns>True on success</returns>
		[RpcExport("Authenticate")]
		private bool RpcAuthenticate(string password)
		{
			// Verify it
			ServiceManager server = Locator.Current.GetService<ServiceManager>();
			m_authenticated = (server.Password == password);
			return m_authenticated;
		}

		/// <summary>
		/// Subscribe to a topic
		/// </summary>
		/// <param name="topic">The topic to subscribe to</param>
		/// <returns>True on success</returns>
		[RpcExport("Subscribe", AuthenticationRequired = true)]
		private bool RpcSubscribe(string topic)
		{
			ITopic target = m_messagebus.Create(topic);
			if (target == null)
				return false;
			m_messagebus.Subscribe(target, this);
			return true;
		}

		/// <summary>
		/// Unsubscribe from a topic
		/// </summary>
		/// <param name="topic">The topic to unsubscribe from</param>
		/// <returns>True on success</returns>
		[RpcExport("Unsubscribe", AuthenticationRequired = true)]
		private bool RpcUnsubscribe(string topic)
		{
			ITopic target = m_messagebus.Create(topic);
			if (target == null)
				return false;
			m_messagebus.Unsubscribe(target, this);
			return true;
		}

		/// <summary>
		/// Get the current server state
		/// </summary>
		/// <returns></returns>
		[RpcExport("GetState", AuthenticationRequired = true)]
		private IDictionary<string, object> RpcGetState()
		{
			return m_mot.Pack();
		}

		/// <summary>
		/// Get the configuration for a specified object
		/// </summary>
		/// <returns></returns>
		[RpcExport("GetConfiguration", AuthenticationRequired = true)]
		private IDictionary<string, object> RpcGetConfiguration(string uuid)
		{
			IUserObject instance = m_mot.GetInstance(Guid.Parse(uuid));
			if (instance == null)
				throw new ArgumentException("No such object.");
			IDictionary<string, object> config = m_mot.GetConfiguration(instance.UUID);
			if (config == null)
				throw new ArgumentException("Object does not have a configuration.");
			// Set up the result
			Dictionary<string, object> result = new Dictionary<string, object>();
			result["active"] = config;
			IConfigurationDescription configDescription = m_mot.GetConfigurationDescription(instance.UUID);
			List<IDictionary<string, object>> details = new List<IDictionary<string, object>>();
			foreach (IConfigurationValue value in configDescription)
				details.Add(value.Pack());
			result["details"] = details;
			return result;
		}

		[RpcExport("SetConfiguration", AuthenticationRequired = true)]
		private IDictionary<string, string> RpcSetConfiguration(string uuid, IDictionary<string, object> config)
		{
			// Make sure the object ID is valid
			IUserObject instance = m_mot.GetInstance(Guid.Parse(uuid));
			if (instance == null)
				throw new ArgumentException("No such object.");
			// Make sure it is an object and it is configurable
			if (instance.ObjectType.IsFactory() || !(instance is IConfigurable))
				throw new ArgumentException("Cannot apply configuration to this type of object.");
			// Get the matching configuration description
			IConfigurationDescription desc = m_mot.GetConfigurationDescription(instance.UUID);
			if (desc == null)
				throw new ArgumentException("No configuration description available for object.");
			// Verify the configuration
			Dictionary<string, string> failures = new Dictionary<string, string>();
			config = desc.Verify(config, failures);
			if (config != null)
			{
				IConfigurable configurable = instance as IConfigurable;
				if (configurable.ValidateConfiguration(desc, config, failures))
					configurable.ApplyConfiguration(desc, config);
			}
			return failures;
		}

		[RpcExport("CreateInstance", AuthenticationRequired = true)]
		private string RpcCreateInstance(string uuid, IDictionary<string, object> config)
		{
			return null;
		}

		[RpcExport("StopServer", AuthenticationRequired = true)]
		private bool RpcStopServer(bool restart)
		{
			return false;
		}
		#endregion
	}
}
