using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SensHub.Plugins;
using Splat;

namespace SensHub.Server
{
	/// <summary>
	/// Implements IFolder which allows for creation of and access to files.
	/// </summary>
	internal class FileSystem : IFolder, IEnableLogger
	{
		// Top level file locations.
		public const string DataFolder = "data";
		public const string PluginFolder = "plugins";
		public const string SiteFolder = "site";
		public const string LogFolder = "logs";

		// Base path and custom locations
		private string m_path;
		private Dictionary<string, IFolder> m_folders;

		/// <summary>
		/// Allow access to the full path this folder represents
		/// </summary>
		public String BasePath
		{
			get { return m_path; }
		}

		/// <summary>
		/// Constructor with a base path
		/// </summary>
		/// <param name="path"></param>
		internal FileSystem(string path)
		{
			// Make sure the base path exists and is a directory
			if (!Directory.Exists(path))
				Directory.CreateDirectory(path);
			// Save state
			m_path = path;
			m_folders = new Dictionary<string, IFolder>();
		}

		/// <summary>
		/// Create (or open) a new file in the folder.
		/// </summary>
		/// <param name="name">
		/// The name of the file to open or create. The file name must not contain path
		/// separators.
		/// </param>
		/// <param name="access">
		/// The access mode to open the file with.
		/// </param>
		/// <param name="options">
		/// Creation options.
		/// </param>
		/// <returns>A Stream instance to read or write to the file.</returns>
		public Stream CreateFile(string name, FileAccessMode access, CreationOptions options)
		{
			bool exists = FileExists(name);
			string target = Path.Combine(m_path, name);
			switch (options)
			{
				case CreationOptions.FailIfExists:
					if (exists)
						throw new InvalidOperationException(String.Format("File '{0}' exists and {1} was specified.", target, options));
					break;
				case CreationOptions.OpenIfExists:
					break;
				case CreationOptions.ReplaceExisting:
					if (exists)
						File.Delete(target);
					break;
			}
			// Now open the stream
			return File.Open(target, FileMode.OpenOrCreate, (access == FileAccessMode.Read) ? FileAccess.Read : FileAccess.ReadWrite, FileShare.None);
		}

		/// <summary>
		/// Test if the given file exists in the folder.
		/// </summary>
		/// <param name="name">
		/// The name of the file to open or create. The file name must not contain path
		/// separators.
		/// </param>
		/// <returns>True if the file exists.</returns>
		public bool FileExists(string name)
		{
			if (Path.GetFileName(name) != name)
				throw new ArgumentException(String.Format("'{0} is not a valid file name", name));
			return File.Exists(Path.Combine(m_path, name));
		}

		public IFolder OpenFolder(string name, bool createIfNotPresent = true)
		{
			lock (m_folders)
			{
				// Do we already have a reference to this folder?
				if (!m_folders.ContainsKey(name))
				{
					// Try and create the folder instance
					try
					{
						string target = Path.Combine(m_path, name);
						if (Directory.Exists(target) || createIfNotPresent)
							m_folders[name] = new FileSystem(target);
						else
							return null;
					}
					catch (Exception ex)
					{
						this.Log().Warn("Cannot create folder '{0}' in '{1}'", name, m_path);
						return null;
					}
				}
				return m_folders[name];
			}
		}
	}

}
