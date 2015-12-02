using System.IO;
using System.Collections.Generic;
using SensHub.Plugins;
using SensHub.Server.Http;
using SensHub.Server.Mqtt;
using CommandLine;
using Splat;

namespace SensHub.Server
{
	class Program
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

        static ConfigurationValue[] ServerConfiguration =
        {
            new ConfigurationValue("mqttServer", ConfigurationValue.ValueType.StringValue, "localhost",
                "Address of the MQTT server to use."),
            new ConfigurationValue("httpPort", ConfigurationValue.ValueType.NumericValue, 8000,
                "Set the port that the HTTP server will listen on.")
        };

		static void Main(string[] args)
		{
			// Set up the logger
			Locator.CurrentMutable.RegisterConstant(new Logger(), typeof(ILogger));
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
            // Load the server configuration and make it globally available
            ConfigurationImpl serverConfig = ConfigurationImpl.Load(
                "SensHub.json",
                new List<ConfigurationValue>(ServerConfiguration).AsReadOnly()
                );
            Locator.CurrentMutable.RegisterConstant(serverConfig, typeof(Configuration));
			// Set up the MessageBus
			MessageBus messageBus = new MessageBus();
			Locator.CurrentMutable.RegisterConstant(messageBus, typeof(IMessageBus));
            // TODO: Initialise logging now we have a server configuration
            // Set up the  HttpServer
            FileSystem sitePath = (FileSystem)fs.OpenFolder("site");
            HttpServer httpServer = new HttpServer(sitePath.BasePath);
            Locator.CurrentMutable.RegisterConstant(httpServer, typeof(HttpServer));
			// Initialise the plugins (internal and user provided)
			PluginManager plugins = new PluginManager();
			FileSystem pluginDir = fs.OpenFolder("plugins") as FileSystem;
			plugins.LoadPlugins(pluginDir.BasePath);
			plugins.InitialisePlugins();
            // Unpack the static site contents and start the HTTP server
			httpServer.UnpackSite();
			httpServer.Start();
			// The MessageBus will run on the main thread until a shutdown is requested
			System.Console.WriteLine("Server running - press any key to quit.");
			messageBus.Run();
			// Clean up
            httpServer.Stop();
        }
	}
}
