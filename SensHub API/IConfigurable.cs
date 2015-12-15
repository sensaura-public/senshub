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
		/// Validate a configuration
		/// 
		/// The SensHub core will validate type conversions but the object itself will
		/// need to ensure the values given are suitable. This method is invoked
		/// prior to applying a configuration in order to ensure that.
		/// </summary>
		/// <param name="description"></param>
		/// <param name="values"></param>
		/// <param name="failures"></param>
		/// <returns></returns>
		bool ValidateConfiguration(IConfigurationDescription description, IDictionary<string, object> values, IDictionary<string, string> failures);

		/// <summary>
		/// Apply a given configuration.
		/// 
		/// The values will have been validated prior to applying.
		/// </summary>
		/// <param name="description">A description of the configuration.</param>
		/// <param name="values">The values for the configuration.</param>
		void ApplyConfiguration(IConfigurationDescription description, IDictionary<string, object> values);
	}
}
