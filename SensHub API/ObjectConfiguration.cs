using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SensHub.Plugins
{
	/// <summary>
	/// Describes the configuration for an object.
	/// </summary>
	public class ObjectConfiguration : IReadOnlyList<ConfigurationValue>
	{
		// Instance variables
		private List<ConfigurationValue> m_configuration;

		/// <summary>
		/// Constructor from a list of configuration values
		/// </summary>
		/// <param name="description"></param>
		public ObjectConfiguration(List<ConfigurationValue> description)
		{
			m_configuration = description;
		}

		/// <summary>
		/// Constructor from an array of configuration values
		/// </summary>
		/// <param name="description"></param>
		public ObjectConfiguration(ConfigurationValue[] description)
		{
			m_configuration = new List<ConfigurationValue>(description);
		}

		/// <summary>
		/// Verify a set of data for the configuration
		/// </summary>
		/// <param name="values"></param>
		/// <param name="failed"></param>
		/// <returns></returns>
		public IDictionary<string, object> Verify(IDictionary<string, object> values, IList<string> failed = null)
		{
			bool success = true;
			Dictionary<string, object> result = new Dictionary<string, object>();
			foreach (ConfigurationValue value in m_configuration)
			{
				object source;
				if (values.ContainsKey(value.DisplayName))
					source = values[value.DisplayName];
				else
					source = value.DefaultValue;
				try
				{
					source = value.Validate(source);
				}
				catch (Exception) {
					source = null;
				}
				if (source == null) 
				{
					success = true;
					if (failed != null)
						failed.Add(value.DisplayName);
				}
				else
					result[value.DisplayName] = source;
			}
			return success?result:null;
		}
		#region Implementation of IReadOnlyList
		public ConfigurationValue this[int index]
		{
			get { return ((IReadOnlyList<ConfigurationValue>)m_configuration)[index]; }
		}

		public int Count
		{
			get { return ((IReadOnlyList<ConfigurationValue>)m_configuration).Count; }
		}

		public IEnumerator<ConfigurationValue> GetEnumerator()
		{
			return ((IReadOnlyList<ConfigurationValue>)m_configuration).GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<ConfigurationValue>)m_configuration).GetEnumerator();
		}
		#endregion

	}
}
