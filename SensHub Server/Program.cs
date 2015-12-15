using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using SensHub.Plugins;
using SensHub.Server.Http;
using SensHub.Server.Mqtt;
using SensHub.Server.Scripting;
using SensHub.Server.Managers;
using CommandLine;
using Splat;

namespace SensHub.Server
{
	class Program : IUserObject, IConfigurable
	{
		private static Guid MyUUID = Guid.Parse("{377ECFA2-2B36-4BBF-8F3F-66C0582DFED8}");
		private const UserObjectType MyType = UserObjectType.Server;

		#region Implementation of IUserObject
		public System.Guid UUID
		{
			get { return MyUUID; }
		}

		public UserObjectType ObjectType
		{
			get { return MyType; }
		}
		#endregion

		#region Implementation of IConfigurable
		public bool ValidateConfiguration(IConfigurationDescription description, IDictionary<string, object> values, IDictionary<string, string> failures)
		{
			// TODO: Implement this
			return true;
		}

		public void ApplyConfiguration(IConfigurationDescription description, IDictionary<string, object> values)
		{
			throw new NotImplementedException();
		}
		#endregion

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

		static void Main(string[] args)
		{
			// Set up the logger
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
			// Make it globally available.
			FileSystem fs = new FileSystem(options.StorageDirectory);
			Locator.CurrentMutable.RegisterConstant(fs, typeof(FileSystem));
			// Set up the MessageBus
			MessageBus messageBus = new MessageBus();
			Locator.CurrentMutable.RegisterConstant(messageBus, typeof(IMessageBus));
			// Set up the Master Object Table
			MasterObjectTable mot = new MasterObjectTable();
			Locator.CurrentMutable.RegisterConstant(mot, typeof(MasterObjectTable));
			mot.AddMetaData(Assembly.GetExecutingAssembly());
			// TODO: Create and add the server instance
			Program server = new Program();
			mot.AddInstance(server);
            // Load the server configuration and make it globally available
// TODO: Reimplement this
//            ConfigurationImpl serverConfig = ConfigurationImpl.Load(
//                "SensHub.json",
//                mot.GetConfigurationDescription(server.UUID)
//                );
//            Locator.CurrentMutable.RegisterConstant(serverConfig, typeof(Configuration));
            // Initialise logging now we have a server configuration
			logger.Enable();
            // Set up the  HttpServer
			string webSite = options.WebDirectory;
			if (webSite == null)
			{
				FileSystem sitePath = (FileSystem)fs.OpenFolder("site");
				webSite = sitePath.BasePath;
			}
            HttpServer httpServer = new HttpServer(webSite);
            Locator.CurrentMutable.RegisterConstant(httpServer, typeof(HttpServer));
			// Initialise the plugins (internal and user provided)
			PluginManager plugins = new PluginManager();
			plugins.AddPlugin(new MqttPlugin());
			plugins.AddPlugin(new WebHookPlugin());
			plugins.AddPlugin(new ScriptPlugin());
			FileSystem pluginDir = fs.OpenFolder("plugins") as FileSystem;
			plugins.LoadPlugins(pluginDir.BasePath);
			plugins.InitialisePlugins();
            // Unpack the static site contents and start the HTTP server
			if (options.WebDirectory == null)
				httpServer.UnpackSite();
            httpServer.UnpackImages();
			httpServer.Start();
			// The MessageBus will run on the main thread until a shutdown is requested
			System.Console.WriteLine("Server running - press any key to quit.");
			messageBus.Run();
			// Clean up
			System.Console.WriteLine("Shutting down ...");
			plugins.ShutdownPlugins();
            httpServer.Stop();
        }
	}
}
