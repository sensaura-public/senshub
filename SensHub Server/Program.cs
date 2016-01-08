using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using IotWeb.Server;
using SensHub.Plugins;
using SensHub.Core;
using SensHub.Core.Plugins;
using SensHub.Core.Messages;
using SensHub.Core.Http;
using SensHub.Server.Scripting;
using CommandLine;
using Splat;

namespace SensHub.Server
{
	class Program : IEnableLogger
	{
		// Define a class to receive parsed values
		class Options
		{
			[Option('d', "debug", DefaultValue = false,
				HelpText = "Include debug information in logs.")]
			public bool Debug { get; set; }
			
			[Option('s', "storage", Required = true,
				HelpText = "Set the storage directory.")]
			public string StorageDirectory { get; set; }

			[Option('w', "website",
				HelpText = "Use a static web site directory instead of the built in version.")]
			public string WebDirectory { get; set; }

			[ParserState]
			public IParserState LastParserState { get; set; }

			[HelpOption(HelpText = "Display this help screen.")]
			public string GetUsage()
			{
				return "Show help.";
//				return HelpText.AutoBuild(this,
//				  (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
			}
		}

/*
		/// <summary>
		/// Populate the plugin list with all plugins found in the specified directory.
		/// 
		/// This method does not initialise the plugins, it simply discovers them and
		/// adds them to master list.
		/// </summary>
		/// <param name="directory"></param>
		public void LoadPlugins(string directory)
		{
			this.Log().Debug("Scanning directory '{0}' for plugins.", directory);
			if (!Directory.Exists(directory))
			{
				this.Log().Warn("Plugin directory '{0}' does not exist.", directory);
				return;
			}
			MasterObjectTable mot = Locator.Current.GetService<MasterObjectTable>();
			String[] files = Directory.GetFiles(directory, "*.dll");
			foreach (string pluginDLL in files)
			{
				// Load the assembly
				Assembly asm = null;
				try
				{
					this.Log().Debug("Attempting to load '{0}'", pluginDLL);
					asm = Assembly.LoadFile(pluginDLL);
				}
				catch (Exception ex)
				{
					this.Log().Error("Failed to load assembly from file '{0}' - {1}", pluginDLL, ex.Message);
					continue;
				}
				// Get the plugins defined in the file (it can have more than one)
				Type[] types = null;
				try
				{
					types = asm.GetTypes();
				}
				catch (Exception ex)
				{
					// TODO: an exception here indicates a plugin built against a different version
					//       of the API. Should report it as such.
					this.Log().Error("Failed to load assembly from file '{0}' - {1}", pluginDLL, ex.Message);
					continue;
				}
				// Load metadata from the assembly
				mot.AddMetaData(asm);
				// Look for plugins
				foreach (var candidate in types)
				{
					if (typeof(AbstractPlugin).IsAssignableFrom(candidate))
					{
						// Go ahead and try to load it
						try
						{
							this.Log().Debug("Creating plugin '{0}.{1}'", candidate.Namespace, candidate.Name);
							AbstractPlugin instance = (AbstractPlugin)Activator.CreateInstance(candidate);
							m_pluginsAvailable[instance.UUID] = instance;
						}
						catch (Exception ex)
						{
							this.Log().Error("Unable to create plugin with class '{0}' in extension '{1}' - {2}",
								candidate.Name,
								pluginDLL,
								ex.ToString()
								);
							continue;
						}
					}
				}
			}
		}
*/

		static void Main(string[] args)
		{
			// Set up the logging for the platform
			Logger logger = new Logger();
			Locator.CurrentMutable.RegisterConstant(logger, typeof(ILogger));
			// Parse command line to get paths
			Options options = new Options();
			if (!Parser.Default.ParseArguments(args, options))
				return;
			// Make sure the storage directory exists
			if (!Directory.Exists(options.StorageDirectory))
			{
                LogHost.Default.Error("Error: The storage directory '{0}' does not exist.", options.StorageDirectory);
				return;
			}
			// Set up the IFolder implementation for the platform
			FileSystem fs = new FileSystem(options.StorageDirectory);
			Locator.CurrentMutable.RegisterConstant(fs, typeof(IFolder));
			// Set up the Master Object Table
			MasterObjectTable mot = new MasterObjectTable();
			Locator.CurrentMutable.RegisterConstant(mot, typeof(MasterObjectTable));
			// Set up the service manager (which doubles as the Server object)
			ServiceManager server = new ServiceManager();
			Locator.CurrentMutable.RegisterConstant(server, typeof(ServiceManager));
			// Add the message bus service
			MessageBus msgBus = new MessageBus();
			Locator.CurrentMutable.RegisterConstant(msgBus, typeof(IMessageBus));
			server.AddServer(msgBus);
			// Add the plugin manager service (this also provides scheduled execution)
			PluginManager plugins = new PluginManager();
			server.AddServer(plugins);
			// TODO: Add dynamic plugins
			// Add and configure the HTTP server
			SensHubHttpServer http = new SensHubHttpServer(new SocketServer(server.HttpPort), true, true);
			server.AddServer(http);
			// Initialise logging
			logger.Enable(server.LogLevel);
			// Run all the services
			server.Start();
			// TODO: Clean up
        }
	}
}
