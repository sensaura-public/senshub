using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

namespace SensHub.Plugins
{
	/// <summary>
	/// Provides the host interface for plugins.
	/// 
	/// The host interface provides limited access to the SensHub server
	/// internals, allowing a plugin to work with the MessageBus, register
	/// actions, filters and sources and manages configuration and data
	/// files for the plugin.
	/// </summary>
    public interface IPluginHost : IMessageBus
    {
		/// <summary>
		/// The version of the SensHub server.
		/// 
		/// This information is provided for reporting purposes, not for
		/// verifying the API (if the API changes old extensions will
		/// failed to load anyway).
		/// </summary>
		Version Version { get; }

		/// <summary>
		/// Specifies the culture (locale) the server is operating with. 
		/// 
		/// Plugins should use this information to customise any strings
		/// returned. If the culture is changed the service will restart
		/// so plugins can assume that this value will remain constant
		/// while they are running.
		/// </summary>
		CultureInfo Culture { get; }

		/// <summary>
		/// Provide access to the plugins storage space.
		/// 
		/// Plugins can read and write arbitrary files inside their
		/// own data directory. This property provides a <see cref="IFolder"/>
		/// instance that enforces that.
		/// </summary>
		IFolder Datastore { get; }

		bool RegisterActionFactory(AbstractActionFactory factory);

		bool RegisterSourceFactory(AbstractSourceFactory factory);
    }
}
