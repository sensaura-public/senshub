using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SensHub.Plugins;
using SensHub.Plugins.Utilities;

namespace SensHub.Server
{
	/// <summary>
	/// Implements IFolder which allows for creation of and access to files.
	/// </summary>
	internal class FolderImpl : IFolder
	{
		/// <summary>
		/// The path this folder is attached to.
		/// </summary>
		private string m_path;

		internal FolderImpl(string path)
		{
			m_path = path;
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
		public Stream CreateFile(string name, SensHub.Plugins.Utilities.FileAccess access, CreationOptions options)
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
			return File.Open(target, FileMode.OpenOrCreate, (access == SensHub.Plugins.Utilities.FileAccess.Read) ? System.IO.FileAccess.Read : System.IO.FileAccess.ReadWrite, FileShare.None);
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
	}


	public class FileSystem : IFileSystem
	{
		// Base path and custom locations
		private string m_basePath;
		private Dictionary<string, string> m_custom;
		private Dictionary<string, IFolder> m_folders;

		public FileSystem(string basePath)
		{
			// Make sure the base path exists and is a directory
			if (!Directory.Exists(basePath))
				Directory.CreateDirectory(basePath);
			// Save state
			m_basePath = basePath;
			m_custom = new Dictionary<string, string>();
			m_folders = new Dictionary<string, IFolder>();
		}

		/// <summary>
		/// Map a physical path to a subdirectory in the virtual file system.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="path"></param>
		public void SetPath(string name, string path)
		{
			// Check parameters
			if (!name.IsValidIdentifier())
				throw new ArgumentException("Invalid directory name");
			// Make sure the target mapping exists
			if (!Directory.Exists(path))
				Directory.CreateDirectory(path);
			// Add it to the list
			m_custom.Add(name, path);
		}

		/// <summary>
		/// Get a system path from the child path name. If a mapping has
		/// been registered that location will be used, otherwise the child
		/// directory will be created under the base path location.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public string GetPath(string name)
		{
			// Check parameters
			if (!name.IsValidIdentifier())
				throw new ArgumentException("Invalid directory name");
			// Do we have a custom mapping ?
			if (m_custom.ContainsKey(name))
				return m_custom[name];
			// Generate from base path
			string target = Path.Combine(m_basePath, name);
			Directory.CreateDirectory(target);
			return target;
		}

		/// <summary>
		/// Open a folder in the virtual storage area
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public IFolder OpenFolder(string name)
		{
			// Create a new IFolder if we don't have one
			if (!m_folders.ContainsKey(name))
				m_folders[name] = new FolderImpl(GetPath(name));
			return m_folders[name];
		}
	}
}
