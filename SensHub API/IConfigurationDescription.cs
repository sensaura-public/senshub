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
		IDictionary<string, object> Verify(IDictionary<string, object> values, IList<string> failed = null);
	}
}
