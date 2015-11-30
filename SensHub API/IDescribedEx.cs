using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Splat;

namespace SensHub.Plugins
{
	/// <summary>
	/// This interface allows an iconic representation along with
	/// a textual description.
	/// </summary>
	public interface IDescribedEx : IDescribed
	{
		/// <summary>
		/// Provide a 64x64 px icon to represent this class.
		/// </summary>
		IBitmap Icon { get; }
	}
}
