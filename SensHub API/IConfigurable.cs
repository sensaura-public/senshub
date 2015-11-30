using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SensHub.Plugins
{
	/// <summary>
	/// This interface implies that an object is configurable.
	/// </summary>
	public interface IConfigurable
	{
		/// <summary>
		/// Describe the configuration supported by this object.
		/// </summary>
		IReadOnlyList<ConfigurationValue> ConfigurationDescription { get; }

		/// <summary>
		/// Apply a given configuration.
		/// </summary>
		/// <param name="configuration">The new configuration.</param>
		void ApplyConfiguration(Configuration configuration);
	}
}
