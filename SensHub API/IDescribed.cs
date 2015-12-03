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
	/// The information provided by the interface is used in the UI so strings
	/// must be human readable and should be in the language specified by the
	/// servers locale.
	/// </summary>
	public interface IDescribed
	{
	}
}
