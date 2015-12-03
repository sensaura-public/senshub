using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SensHub.Plugins;
using Splat;

namespace SensHub.Server
{
	public class Logger : ILogger
	{
		private struct LogTarget
		{
			public ITopic m_topic;
			public StreamWriter m_output;
		}

		// Regular expression to match sources
		private static readonly Regex SourceRegex = new Regex(@"^[a-zA-Z0-9\-_]+: ");

		// Instance variables
		private Dictionary<LogLevel, LogTarget> m_targets;
		private MessageBuilder m_builder;
		private DateTime m_lastLogOpen;

		public LogLevel Level { get; set; }

		/// <summary>
		/// Default constructor.
		/// </summary>
		public Logger()
		{
			Level = LogLevel.Warn;
			m_builder = new MessageBuilder();
		}

		public void Enable()
		{
			lock (this)
			{
				// Setup (or reinitialise) the target map
				if (m_targets != null)
				{
					// Close any open files
					foreach (LogTarget target in m_targets.Values)
					{
						if (target.m_output != null)
							target.m_output.Close();
					}
					m_targets.Clear();
				}
				else
					m_targets = new Dictionary<LogLevel, LogTarget>();
				// Get the log level from the server configuration
				LogLevel logLevel;
				Configuration serverConfig = Locator.Current.GetService<Configuration>();
				if (!Enum.TryParse<LogLevel>(serverConfig["logLevel"].ToString(), out logLevel))
					logLevel = LogLevel.Warn;
				Level = logLevel;
				// Set up the targets
				FileSystem fs = Locator.Current.GetService<FileSystem>();
				fs = (FileSystem)fs.OpenFolder("logs");
				string logFile = Path.Combine(fs.BasePath, String.Format("senshub-{0:yyyy-MM-dd}.log", DateTime.Now));
				StreamWriter output = new StreamWriter(File.Open(logFile, FileMode.Append, FileAccess.Write, FileShare.Read));
				IMessageBus messageBus = Locator.Current.GetService<IMessageBus>();
				ITopic logBase = messageBus.Private.Create("server/notifications");
				foreach (LogLevel level in Enum.GetValues(typeof(LogLevel)))
				{
					if (level < logLevel)
						continue;
					LogTarget target = new LogTarget();
					target.m_output = output;
					if (level >= LogLevel.Warn)
						target.m_topic = logBase.Create(level.ToString().ToLower() + "s");
					m_targets.Add(level, target);
				}
				m_lastLogOpen = DateTime.Now;
			}
		}

		public void Write(string message, LogLevel logLevel)
		{
			// If we haven't been configured we just send the output to the console
			if (m_targets == null)
			{
				System.Console.WriteLine("{0}:{1}", logLevel, message);
				return;
			}
			// Get the target information
			if (!m_targets.ContainsKey(logLevel))
				return;
			LogTarget target = m_targets[logLevel];
			// Split the source out of the message (if it exists)
			lock (this)
			{
				DateTime now = DateTime.Now;
				Match match = SourceRegex.Match(message);
				string source = "";
				if (match.Success)
				{
					source = message.Substring(0, match.Length - 2);
					message = message.Substring(match.Length);
				}
				if (target.m_output != null)
				{
					target.m_output.WriteLine("{0:hh:mm:ss} {1} ({2}) {3}", now, logLevel, source, message);
					target.m_output.Flush();
				}
				if (target.m_topic != null)
				{
					IMessageBus messageBus = Locator.Current.GetService<IMessageBus>();
					m_builder.Add("logLevel", logLevel.ToString());
					m_builder.Add("source", source);
					m_builder.Add("message", message);
					messageBus.Publish(target.m_topic, m_builder.CreateMessage());
				}
				// Check for log file rollover
				if (now.DayOfYear != m_lastLogOpen.DayOfYear)
					Enable();
			}
		}
	}
}
