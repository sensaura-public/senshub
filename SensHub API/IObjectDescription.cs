using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Splat;

namespace SensHub.Plugins
{
	/// <summary>
	/// Provides a description for the object.
	/// 
	/// Descriptions provide the information needed to display the
	/// object in the UI.
	/// </summary>
	public interface IObjectDescription : IPackable
	{
		/// <summary>
		/// Provide a 48x48 px icon to represent this class.
		/// </summary>
		string Icon { get; }

		/// <summary>
		/// Provide a short name for the class.
		/// 
		/// This name is used as a title for things like configuration and
		/// property pages.
		/// </summary>
		string DisplayName { get; }

		/// <summary>
		/// Get the short (generally one paragraph) description of the class.
		/// </summary>
		string Description { get; }

		/// <summary>
		/// Provide a more detailed description of the class.
		/// 
		/// This is a multi line description used to provide more detail.
		/// </summary>
		string DetailedDescription { get; }
	}
}
