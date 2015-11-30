using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SensHub.Plugins
{
	/// <summary>
	/// Represents a configuration.
	/// </summary>
	public class Configuration : IPackable, IDictionary<string, object>
	{
		private Dictionary<string, object> m_values;
		private Dictionary<string, ConfigurationValue> m_defaults;
		private IReadOnlyList<ConfigurationValue> m_description;

		/// <summary>
		/// Describe the configuration supported by this object.
		/// </summary>
		IReadOnlyList<ConfigurationValue> ConfigurationDescription
		{
			get { return m_description; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="description">The description of the configuration.</param>
		/// <param name="values">The values changed from default.</param>
		public Configuration(IReadOnlyList<ConfigurationValue> description, IDictionary<string, object> values)
		{
			// Save the description
			m_description = description;
			// Build the dictionary of default values
			m_defaults = new Dictionary<string, ConfigurationValue>();
			foreach (ConfigurationValue value in description)
			{
				m_defaults[value.DisplayName] = value;
			}
			// Build the dictionary of changed values
			m_values = new Dictionary<string, object>();
			if (values != null)
			{
				foreach (string key in values.Keys)
				{
					if (m_defaults.ContainsKey(key))
						m_values[key] = values[key];
				}
			}
		}

		/// <summary>
		/// Revert the configuration to default values.
		/// </summary>
		public void Revert()
		{
			lock (m_values)
			{
				m_values.Clear();
			}
		}

		#region Implementation of IPackable
		/// <summary>
		/// When the configuration is serialised only modified values will be saved.
		/// </summary>
		/// <returns>A dictionary of modified values.</returns>
		public IReadOnlyDictionary<string, object> Pack()
		{
			return (IReadOnlyDictionary<string, object>)m_values;
		}
		#endregion

		#region Implementation of IDictionary
		/// <summary>
		/// Update a value in the configuration.
		/// 
		/// This fails with an exception if the value is not defined in the
		/// configuration description or if validation of the data fails.
		/// </summary>
		/// <param name="key">Name of the configuration value to change.</param>
		/// <param name="value">The new value to assign to the value.</param>
		public void Add(string key, object value)
		{
			if (!m_defaults.ContainsKey(key))
				throw new InvalidOperationException();
			// Validate the incoming data
			value = m_defaults[key].Validate(value);
			if (value == null)
				throw new ArgumentException("Configuration value could not be validated.");
			// Update the value
			lock (m_values)
			{
				m_values[key] = value;
			}
		}

		/// <summary>
		/// Determine if the key is present in the configuration.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public bool ContainsKey(string key)
		{
			return m_defaults.ContainsKey(key);
		}

		/// <summary>
		/// Return the keys as a collection.
		/// </summary>
		public ICollection<string> Keys
		{
			get { return m_defaults.Keys; }
		}

		/// <summary>
		/// Remove a value from the configuration.
		/// 
		/// This is an unsupported operation for configurations.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public bool Remove(string key)
		{
			throw new InvalidOperationException();
		}

		/// <summary>
		/// Try and get a value from the collection.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public bool TryGetValue(string key, out object value)
		{
			value = null;
			lock (m_values)
			{
				if (m_values.ContainsKey(key))
				{
					value = m_values[key];
					return true;
				}
			}
			if (m_defaults.ContainsKey(key))
			{
				value = m_defaults[key].DefaultValue;
				return true;
			}
			return false;
		}

		/// <summary>
		/// Return the set of all values as a collection.
		/// </summary>
		public ICollection<object> Values
		{
			get 
			{
				List<object> values = new List<object>();
				lock (m_values)
				{
					foreach (string key in m_defaults.Keys)
					{
						if (m_values.ContainsKey(key))
							values.Add(m_values[key]);
						else
							values.Add(m_defaults[key].DefaultValue);
					}

				}
				return values;
			}
		}

		public object this[string key]
		{
			get
			{
				throw new NotImplementedException();
			}
			set
			{
				throw new InvalidOperationException();
			}
		}

		/// <summary>
		/// Add a new Key/Value pair to the collection.
		/// </summary>
		/// <param name="item"></param>
		public void Add(KeyValuePair<string, object> item)
		{
			Add(item.Key, item.Value);
		}

		/// <summary>
		/// Clear the collection.
		/// 
		/// Not supported for configurations.
		/// </summary>
		public void Clear()
		{
			// Operation not supported
			throw new InvalidOperationException();
		}

		/// <summary>
		/// Determine if the configuration contains the given key/value pair
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		public bool Contains(KeyValuePair<string, object> item)
		{
			// Operation not supported
			// TODO: Could implement this if needed.
			throw new InvalidOperationException();
		}

		public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Get the number of entries in the configuration
		/// </summary>
		public int Count
		{
			get { return m_defaults.Count; }
		}

		/// <summary>
		/// Determine if this is a read only collection
		/// </summary>
		public bool IsReadOnly
		{
			get { return false; }
		}

		/// <summary>
		/// Remove a value from the collection.
		/// 
		/// Not supported for configurations.
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		public bool Remove(KeyValuePair<string, object> item)
		{
			throw new InvalidOperationException();
		}

		/// <summary>
		/// Iterate over the key/value pairs in the configuration.
		/// </summary>
		/// <returns></returns>
		public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Get an enumerator for the collection.
		/// </summary>
		/// <returns></returns>
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			throw new NotImplementedException();
		}
		#endregion

	}
}
