using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SensHub.Plugins.Utilities;
using SensHub.Server.Services;
using Splat;

namespace SensHub.Server
{
	class Program
	{
		static void Main(string[] args)
		{
			// TODO: Parse command line to get paths
			// Register the singletons
			Locator.CurrentMutable.RegisterConstant(new Logger(), typeof(ILogger));
			Locator.CurrentMutable.RegisterConstant(new FileSystem(@"C:\\Shane\\Scratch\\senshub"), typeof(IFileSystem));
			// Get the system configuration
			Configuration config = Configuration.Open("system");

			// Initialise the plugins (internal and user provided)
			PluginManager plugins = new PluginManager();
			plugins.LoadPlugins(@"C:\\Shane\\Scratch\\senshub\\plugins");
			plugins.InitialisePlugins();
		}

		static void bus_MessageReceived(Topic topic, Message message)
		{
			System.Console.WriteLine("Message received on topic '{0}'", topic);
		}
	}
}
