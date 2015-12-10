using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SensHub.Plugins
{
	/// <summary>
	/// This interface is used to mark classes that can describe themselves.
	///
    /// Classes that use this interface must have a matching 'description'
    /// entry in the metadata.xml file for the assembly.
	/// </summary>
	public interface IDescribed
	{
	}
}
