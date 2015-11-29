using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Sensaura.Utilities;
using Sensaura.Services;
using Splat;

namespace Sensaura.Hub
{
	class Program
	{
		static void Main(string[] args)
		{
			// TODO: Parse command line to get paths
			// Register the singletons
			Locator.CurrentMutable.RegisterConstant(new FileSystem(@"C:\\Shane\\Scratch\\senshub"), typeof(IFileSystem));
			// Get the system configuration
			Configuration config = Configuration.Open("system");

			// SWG: TESTING
			MessageBus bus = MessageBus.Instance;
			bus.MessageReceived += bus_MessageReceived;
			Topic child = bus.CreateTopic("this/isa/topic");
			child.MessageReceived += bus_MessageReceived;
			System.Console.WriteLine("Topic = '{0}'", child.ToString());
			// Send a message
			MessageBuilder builder = new MessageBuilder();
			Message message = builder.CreateMessage();
			child.Publish(message);
			Thread.Sleep(5000);
		}

		static void bus_MessageReceived(Topic topic, Message message)
		{
			System.Console.WriteLine("Message received on topic '{0}'", topic);
		}
	}
}
