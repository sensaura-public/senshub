using System;
using System.IO;

namespace SensHub.Plugins
{
	/// <summary>
	/// File access mode
	/// </summary>
	public enum FileAccessMode {
		/// <summary>
		/// Open an existing file for read only access.
		/// </summary>
		Read,

		/// <summary>
		/// Open the file for read and write operations.
		/// </summary>
		ReadAndWrite
	}

	/// <summary>
	/// File and folder creation options.
	/// </summary>
	public enum CreationOptions {
		/// <summary>
		/// Raise an exception if the file already exists.
		/// </summary>
		FailIfExists,
		/// <summary>
		/// Open the existing file.
		/// </summary>
		OpenIfExists,
		/// <summary>
		/// Replace an existing file with a new (empty) one.
		/// </summary>
		ReplaceExisting
	}

	/// <summary>
	/// Represents a folder in the application storage area
	/// </summary>
	public interface IFolder
	{
		/// <summary>
		/// Open a folder in the storage area.
		/// 
		/// The folder will be created if it does not exist.
		/// </summary>
		/// <param name="name">
		/// The name of the folder to open. The name must not contain path
		/// separators.
		/// </param>
		/// <param name="createIfNotPresent">
		/// If this parameter is 'true' (default) the folder will be created if it
		/// is not already present.
		/// </param>
		/// <returns>An IFolder instance representing the folder.</returns>
		IFolder OpenFolder(string name, bool createIfNotPresent = true);

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
		Stream CreateFile(string name, FileAccessMode access, CreationOptions options);

		/// <summary>
		/// Test if the given file exists in the folder.
		/// </summary>
		/// <param name="name">
		/// The name of the file to open or create. The file name must not contain path
		/// separators.
		/// </param>
		/// <returns>True if the file exists.</returns>
		bool FileExists(string name);
	}

	public static class FolderExtensions
	{
		/// <summary>
		/// Create all child folders in a path.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="path"></param>
		/// <returns></returns>
		public static IFolder CreateChildren(this IFolder parent, string path)
		{
			// Split the path based on separators ('/' and '\')
			string[] parts = path.Split(new char[] { '/', '\\' });
			IFolder result = parent;
			foreach (string part in parts)
				result = result.OpenFolder(part);
			return result;
		}

		/// <summary>
		/// Create all child folders in a path.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="path"></param>
		/// <returns></returns>
		public static IFolder OpenChild(this IFolder parent, string path)
		{
			// Split the path based on separators ('/' and '\')
			string[] parts = path.Split(new char[] { '/', '\\' });
			IFolder result = parent;
			foreach (string part in parts)
			{
				result = result.OpenFolder(part, false);
				if (result == null)
					break;
			}
			return result;
		}

	}

}
