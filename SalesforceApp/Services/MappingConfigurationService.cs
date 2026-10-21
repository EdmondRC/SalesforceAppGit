using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Text.Json;
using SalesforceApp.Enums;
using SalesforceApp.Mapping;

namespace SalesforceApp.Services
{
    /// <summary>
    /// Persists named column-mapping configurations to a JSON file so they can be
    /// reused across uploads and across application restarts.
    /// </summary>
    public class MappingConfigurationService
    {
        private readonly string _configFilePath;
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            WriteIndented = true
        };

        public MappingConfigurationService(string? configFilePath = null)
        {
            _configFilePath = configFilePath ?? GetDefaultConfigFilePath();
        }

        private static string GetDefaultConfigFilePath()
        {
            // Store alongside other app data so the file survives app restarts/deployments
            // (unlike the %TEMP% folder used for uploaded spreadsheets).
            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "SalesforceAccountMatching");

            Directory.CreateDirectory(folder);

            return Path.Combine(folder, "mapping-configurations.json");
        }

        public List<MappingConfiguration> GetAll()
        {
            if (!File.Exists(_configFilePath))
            {
                return new List<MappingConfiguration>();
            }

            var json = File.ReadAllText(_configFilePath);

            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<MappingConfiguration>();
            }

            return JsonSerializer.Deserialize<List<MappingConfiguration>>(json)
                ?? new List<MappingConfiguration>();
        }

        public MappingConfiguration? GetByName(string name)
        {
            return GetAll()
                .FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Saves the given configuration, overwriting any existing configuration with the same name.
        /// </summary>
        public void Save(MappingConfiguration configuration)
        {
            var configurations = GetAll();

            configurations.RemoveAll(c =>
                string.Equals(c.Name, configuration.Name, StringComparison.OrdinalIgnoreCase));

            configurations.Add(configuration);

            var json = JsonSerializer.Serialize(configurations, SerializerOptions);

            File.WriteAllText(_configFilePath, json);
        }

        public void Delete(string name)
        {
            var configurations = GetAll();

            var removed = configurations.RemoveAll(c =>
                string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));

            if (removed > 0)
            {
                var json = JsonSerializer.Serialize(configurations, SerializerOptions);
                File.WriteAllText(_configFilePath, json);
            }
        }
    }
}