using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SensHub.Plugins
{
	/// <summary>
	/// Describes a configuration for a IConfigurable
	/// </summary>
	public interface IConfigurationDescription : IReadOnlyList<IConfigurationValue>
	{
		/// <summary>
		/// Verify the configuration values provided against the description.
		/// 
		/// This may modify the values (doing type conversion etc) as part of the
		/// verification process.
		/// </summary>
		/// <param name="values"></param>
		/// <param name="failed"></param>
		/// <returns></returns>
		IDictionary<string, object> Verify(IDictionary<string, object> values, IList<string> failed = null);

		/// <summary>
		/// Get the value applied to the named property.
		/// 
		/// This will return the configured value or the default if it not set.
		/// </summary>
		/// <param name="values"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		object GetAppliedValue(IDictionary<string, object> values, string name);
	}
}
