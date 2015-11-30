using System;
using Splat;

namespace SensHub.Server
{
	public class Logger : ILogger
	{
		public LogLevel Level { get; set; }

		public Logger()
		{
#if DEBUG
			Level = LogLevel.Debug;
#else
			Level = LogLevel.Info;
#endif
		}

		public void Write(string message, LogLevel logLevel)
		{
			if (logLevel < Level)
				return;
			// Everything goes to the console
			System.Console.WriteLine("{0}:{1}", logLevel, message);
			// TODO: Queue up log messages to be dispatched as events
		}
	}
}
