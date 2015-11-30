using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SensHub.Plugins;
using SensHub.Plugins.Utilities;
using SensHub.Server.Services;
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
                "Address of the MQTT server to use.")
        };

		static void Main(string[] args)
		{
			// Set up the logger
			Locator.CurrentMutable.RegisterConstant(new Logger(), typeof(ILogger));
			// Parse command line to get paths
			Options options = new Options();
			if (!CommandLine.Parser.Default.ParseArguments(args, options))
				return;
			// Make sure the storage directory exists
			if (!Directory.Exists(options.StorageDirectory))
			{
				System.Console.WriteLine("Error: The storage directory '{0}' does not exist.", options.StorageDirectory);
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
            System.Console.WriteLine("mqttServer = {0}", serverConfig["mqttServer"]);
            serverConfig["mqttServer"] = "127.0.0.1";
            serverConfig.Save();
            Locator.CurrentMutable.RegisterConstant(serverConfig, typeof(Configuration));
			// Initialise the plugins (internal and user provided)
			PluginManager plugins = new PluginManager();
			FileSystem pluginDir = fs.OpenFolder("plugins") as FileSystem;
			plugins.LoadPlugins(pluginDir.BasePath);
			plugins.InitialisePlugins();
		}

		static void bus_MessageReceived(Topic topic, Message message)
		{
			System.Console.WriteLine("Message received on topic '{0}'", topic);
		}
	}
}
