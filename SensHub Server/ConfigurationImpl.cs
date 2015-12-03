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
    internal class ConfigurationImpl : Configuration
    {
        // Directory where configuration files are kept
        private const string ConfigurationDirectory = "config";

        // Name of the backing file for the configuration
        private string m_file;

        protected ConfigurationImpl(IReadOnlyList<ConfigurationValue> description, IDictionary<string, object> values) : base(description, values)
        {
            // Do nothing in this constructor
        }

        public static ConfigurationImpl Load(string filename, IReadOnlyList<ConfigurationValue> description)
        {
            FileSystem configDir = Locator.Current.GetService<FileSystem>();
            configDir = (FileSystem)configDir.OpenFolder(ConfigurationDirectory);
            Dictionary<string, object> values = null;
            if (configDir.FileExists(filename))
                values = ObjectPacker.UnpackRaw(configDir.CreateFile(filename, FileAccessMode.Read, CreationOptions.OpenIfExists));
            ConfigurationImpl result = new ConfigurationImpl(description, values);
            result.m_file = filename;
            return result;
        }

        public override void Save()
        {
            // Get a JSON version of the current configuration
            string json = ObjectPacker.Pack(this);
            // Now save it
            FileSystem configDir = Locator.Current.GetService<FileSystem>();
            configDir = (FileSystem)configDir.OpenFolder(ConfigurationDirectory);
            StreamWriter writer = new StreamWriter(
                configDir.CreateFile(
                    m_file,
                    FileAccessMode.ReadAndWrite,
                    CreationOptions.ReplaceExisting
                    )
                );
            writer.Write(json);
            writer.Close();
        }
    }
}
