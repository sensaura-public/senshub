using System;
using System.IO;

namespace SensHub.Plugins.Utilities
{
	/// <summary>
	/// File access mode
	/// </summary>
	public enum FileAccess {
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
		Stream CreateFile(string name, FileAccess access, CreationOptions options);

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

	/// <summary>
	/// Represents the application storage area.
	/// </summary>
	public interface IFileSystem
	{
		/// <summary>
		/// Open a folder in the storage area.
		/// </summary>
		/// <param name="name">
		/// The name of the folder to open. The name must not contain path
		/// separators.
		/// </param>
		/// <returns>An IFolder instance representing the folder.</returns>
		IFolder OpenFolder(string name);
	}

}
