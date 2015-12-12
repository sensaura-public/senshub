using System;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SensHub.Plugins;
using SensHub.Server;
using SensHub.Server.Http;
using Splat;

namespace SensHub.Server.Managers
{
    /// <summary>
    /// This class manages all the IUserObject instances in the system
	/// 
	/// The master object table maintains information about all IUserObject
	/// instances that are active in the running system. It provides a
	/// single source for all information about these objects - descriptions,
	/// configuration information and configuration data.
    /// </summary>
    public class MasterObjectTable : IPackable, IEnableLogger
    {
		//--- Instance variables
		private Dictionary<Guid, IUserObject> m_instances;
		private Dictionary<string, IObjectDescription> m_descriptions = new Dictionary<string, IObjectDescription>();
		private Dictionary<string, ObjectConfiguration> m_configinfo = new Dictionary<string, ObjectConfiguration>();

        public List<Assembly> Assemblies { get; private set; }

		/// <summary>
		/// Constructor
		/// </summary>
        public MasterObjectTable()
        {
			m_instances = new Dictionary<Guid, IUserObject>();
			m_descriptions = new Dictionary<string, IObjectDescription>();
			m_configinfo = new Dictionary<string, ObjectConfiguration>();
            Assemblies = new List<Assembly>();
        }

        /// <summary>
        /// Pack the table in a form suitable for RPC
        /// </summary>
        /// <returns></returns>
        public IDictionary<string, object> Pack()
        {
			Dictionary<string, object> mot = new Dictionary<string, object>();
			// Add entries for all types
			foreach (string type in Enum.GetNames(typeof(UserObjectType)))
				mot[type] = new Dictionary<string, object>();
			// Now add all the instances
			lock (m_instances)
			{
				foreach (IUserObject instance in m_instances.Values)
				{
					// We just want the description for each object
					IDictionary<string, object> detail = GetDescription(instance.UUID).Pack();
					// For IUserCreatableObjects we add the parent as well
					IUserCreatableObject creatable = instance as IUserCreatableObject;
					if (creatable != null)
						detail["ParentUUID"] = creatable.ParentUUID.ToString();
					// Add it to the appropriate spot
					Dictionary<string, object> container = mot[instance.ObjectType.ToString()] as Dictionary<string, object>;
					container[instance.UUID.ToString()] = detail;
				}
			}
			// All done
			return mot;
		}

		#region Initial Setup
		/// <summary>
		/// Add an object instance to the master table
		/// </summary>
		/// <param name="instance"></param>
		public bool AddInstance(IUserObject instance)
		{
			lock (m_instances)
			{
				if (m_instances.ContainsKey(instance.UUID))
				{
					if (m_instances[instance.UUID] != instance)
					{
						this.Log().Warn("Object '{0}' is already registered with a different instance.", instance.UUID);
						return false;
					}
					return true;
				}
				// Add and test for supporting metadata
				m_instances[instance.UUID] = instance;
				// TODO: Make sure we have a description and a configuration for the instance
				if (GetDescription(instance.UUID) != null)
				{
                    // Does this instance have configuration information?
                    if ((instance as IConfigurable) != null)
                    {
                        if (GetConfigurationDescription(instance.UUID) == null)
                            this.Log().Warn("Object '{0}' has no configuration information. Will not add.", instance.UUID);
                        else
                            return true;
                    }
                    else
                        return true;
				}
				else
					this.Log().Warn("Object '{0}' has no description information. Will not add.", instance.UUID);
				// Not enough data, remove it
				m_instances.Remove(instance.UUID);
				return false;
			}
		}

		/// <summary>
		/// Remove an instance from the master table.
		/// </summary>
		/// <param name="uuid"></param>
		public bool RemoveInstance(Guid uuid)
		{
			lock (m_instances)
			{
				if (!m_instances.ContainsKey(uuid))
					return false;
				// Get the instance and see what other steps are needed
				IUserObject instance = m_instances[uuid];
				if (!instance.ObjectType.IsDeletable())
					return false;
				// TODO: Remove the configuration
				// TODO: Remove the description
				// Finally we need to remove the instance
				m_instances.Remove(uuid);
				return true;
			}
		}

		/// <summary>
		/// Add a description for a given class name (or ID)
		/// </summary>
		/// <param name="clsName"></param>
		/// <param name="description"></param>
		public void AddDescription(string clsName, IObjectDescription description)
		{
			lock (m_descriptions)
			{
				if (m_descriptions.ContainsKey(clsName))
					this.Log().Warn("Description already registered for class '{0}'", clsName);
				else
					m_descriptions[clsName] = description;
			}
		}

		/// <summary>
		/// Add a configuration description for a given class name
		/// </summary>
		/// <param name="clsName"></param>
		/// <param name="configuration"></param>
		public void AddConfigurationDescription(string clsName, ObjectConfiguration configuration)
		{
			lock (m_configinfo)
			{
				if (m_configinfo.ContainsKey(clsName))
					this.Log().Warn("Configuration information already registered for class '{0}'", clsName);
				else
					m_configinfo[clsName] = configuration;
			}
		}

		/// <summary>
		/// Add all the metadata (descriptions and configuration descriptions)
		/// from an assembly.
		/// </summary>
		/// <param name="assembly"></param>
		public void AddMetaData(Assembly assembly)
		{
            Assemblies.Add(assembly);
			Stream source = assembly.GetManifestResourceStream(assembly.GetName().Name + ".Resources.metadata.xml");
			if (source == null)
			{
				LogHost.Default.Warn("Could not find metadata resource for assembly {0}.", assembly.GetName().Name);
				return;
			}
			MetadataParser.LoadFromStream(assembly.GetName().Name, source);
		}
		#endregion

		#region General Access
		/// <summary>
		/// Get an object instance given a UUID
		/// </summary>
		/// <param name="forInstance"></param>
		/// <returns></returns>
		public IUserObject GetInstance(Guid forInstance)
		{
			lock (m_instances)
			{
				if (!m_instances.ContainsKey(forInstance))
					return null;
				return m_instances[forInstance];
			}
		}

		/// <summary>
		/// Get an object instance given a UUID
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="forInstance"></param>
		/// <returns></returns>
		public T GetInstance<T>(Guid forInstance) where T : IUserObject
		{
			IUserObject instance = GetInstance(forInstance);
			if (typeof(T).IsAssignableFrom(instance.GetType()))
				return (T)instance;
			return default(T);
		}

		/// <summary>
		/// Get the description for the instance
		/// </summary>
		/// <param name="forInstance"></param>
		/// <returns></returns>
		public IObjectDescription GetDescription(Guid forInstance)
		{
			IUserObject instance = GetInstance(forInstance);
			if (instance == null)
				return null;
			lock (m_descriptions)
			{
				if (instance is IUserCreatableObject)
				{
					if (!m_descriptions.ContainsKey(instance.UUID.ToString()))
					{
						this.Log().Warn("Object '{0}' exists but has no description.", instance.UUID);
						return null;
					}
					return m_descriptions[instance.UUID.ToString()];
				}
				// Use the fully qualified class name to get the description
				string fqcn = instance.GetType().Namespace + "." + instance.GetType().Name;
				if (!m_descriptions.ContainsKey(fqcn))
				{
					this.Log().Warn("Expected to find description for class '{0}'.", fqcn);
					return null;
				}
				return m_descriptions[fqcn];
			}
		}

		/// <summary>
		/// Get the configuration for the instance
		/// </summary>
		/// <param name="forInstance"></param>
		/// <returns></returns>
		public Configuration GetConfiguration(Guid forInstance)
		{
			IUserObject instance = GetInstance(forInstance);
			if (instance == null)
			{
				this.Log().Warn("Requested configuration for non-existant object '{0}'", forInstance);
				return null;
			}
			IConfigurable configurable = instance as IConfigurable;
			if (configurable == null)
			{
				this.Log().Warn("Requested configuration for unconfigurable object '{0}' (Class {1}.{2})", forInstance, instance.GetType().Namespace, instance.GetType().Name);
				return null;
			}
			ObjectConfiguration configuration = GetConfigurationDescription(forInstance);
			if (configuration == null) {
				this.Log().Warn("No configuration description for object '{0}' (Class {1}.{2})", forInstance, instance.GetType().Namespace, instance.GetType().Name);
				return null;
			}
			return ConfigurationImpl.Load(forInstance.ToString() + ".json", configuration);
		}

		/// <summary>
		/// Get the configuration description for the instance
		/// </summary>
		/// <param name="forInstance"></param>
		/// <returns></returns>
		public ObjectConfiguration GetConfigurationDescription(Guid forInstance)
		{
			IUserObject instance = GetInstance(forInstance);
			if (instance == null)
				return null;
			// User creatable object use the parent to provide the description
			if (instance is IUserCreatableObject)
				return GetConfigurationDescription((instance as IUserCreatableObject).ParentUUID);
			// Look it up
			lock (m_configinfo)
			{
				// Use the fully qualified class name to get the config info
				string fqcn = instance.GetType().Namespace + "." + instance.GetType().Name;
				if (!m_configinfo.ContainsKey(fqcn))
				{
					this.Log().Warn("Could not find configuration information for class '{0}'", fqcn);
					return null;
				}
				return m_configinfo[fqcn];
			}
		}
		#endregion

		#region RPC Interface
		/// <summary>
		/// Get the current system state
		/// </summary>
		/// <returns></returns>
		[RpcCall("GetState", AuthenticationRequired = true)]
		public IDictionary<string, object> RpcGetState()
		{
			return Pack();
		}

		/// <summary>
		/// Get the configuration description and current configuration for an object
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		[RpcCall("GetConfiguration", AuthenticationRequired = true)]
		public IDictionary<string, object> RpcGetConfiguration(string id)
		{
			IUserObject instance = GetInstance(Guid.Parse(id));
			if (instance == null)
				throw new ArgumentException("No such object.");
			Configuration config = GetConfiguration(instance.UUID);
			if (config == null)
				throw new ArgumentException("Object does not have a configuration.");
			// Set up the result
			Dictionary<string, object> result = new Dictionary<string, object>();
			result["active"] = config.Pack();
            ObjectConfiguration configDescription = GetConfigurationDescription(instance.UUID);
            List<IDictionary<string, object>> details = new List<IDictionary<string, object>>();
            foreach (ConfigurationValue value in configDescription)
                details.Add(value.Pack());
			result["details"] = details;
			return result;
		}

		/// <summary>
		/// Apply configuration changes to an object.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="config"></param>
		/// <returns></returns>
		[RpcCall("SetConfiguration", AuthenticationRequired = true)]
		public bool RpcSetConfiguration(string id, IDictionary<string, object> config)
		{
			// TODO: Implement this
			throw new NotImplementedException("This method is not yet implemented.");
		}

		/// <summary>
		/// Create a new object with the given configuration
		/// </summary>
		/// <param name="parentID"></param>
		/// <param name="config"></param>
		/// <returns></returns>
		[RpcCall("CreateInstance", AuthenticationRequired = true)]
		public bool RpcCreateInstance(string parentID, IDictionary<string, object> config)
		{
			// TODO: Implement this
			throw new NotImplementedException("This method is not yet implemented.");
		}
		#endregion

	}
}
