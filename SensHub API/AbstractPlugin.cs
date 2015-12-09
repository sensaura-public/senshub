using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SensHub.Plugins
{
    public abstract class AbstractPlugin : IUserObject
    {
        private const UserObjectType MyType = UserObjectType.Plugin;

        /// <summary>
        /// Always returns Plugin
        /// </summary>
        public UserObjectType ObjectType { get { return MyType; } }

        /// <summary>
        /// The unique ID for this plugin.
        /// </summary>
        public abstract Guid UUID { get; }

        /// <summary>
        /// The version of this plugin. The version information is only used
        /// for display purposes, no checking or validation is done on the value.
        /// </summary>
        public abstract Version Version { get; }

		/// <summary>
		/// Initialise the plugin.
		/// 
		/// Every plugin must have a default constructor but no meaningful 
		/// initialisation should be done there. This initialisation function
		/// is passed a <see cref="IPluginHost"/> instance which allows the
		/// plugin to interact with the SensHub server.
		/// </summary>
		/// <param name="host">
		/// An instance of IPluginHost that the plugin uses to interact with
		/// host services. The plugin should keep a reference to this instance
		/// for use later in it's lifetime.
		/// </param>
		/// <returns>true on success, false on failure.</returns>
		public abstract bool Initialise(IPluginHost host);

		/// <summary>
		/// Shut down the plugin.
		/// 
		/// When this method is invoked the plugin must unregister all
		/// extensions, ensure any cached data is saved and revert to
		/// an inactive state.
		/// </summary>
		public abstract void Shutdown();
    }
}
