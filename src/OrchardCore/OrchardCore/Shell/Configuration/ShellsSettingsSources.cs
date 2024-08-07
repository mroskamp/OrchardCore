using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace OrchardCore.Environment.Shell.Configuration
{
    public class ShellsSettingsSources : IShellsSettingsSources
    {
        private readonly string _tenants;

        public ShellsSettingsSources(IOptions<ShellOptions> shellOptions)
        {
            _tenants = Path.Combine(shellOptions.Value.ShellsApplicationDataPath, "tenants.json");
        }

        public Task AddSourcesAsync(IConfigurationBuilder builder)
        {
            builder.AddTenantJsonFile(_tenants, optional: true);
            return Task.CompletedTask;
        }

        public Task AddSourcesAsync(string tenant, IConfigurationBuilder builder) => AddSourcesAsync(builder);

        public async Task SaveAsync(string tenant, IDictionary<string, string> data)
        {
            JsonObject tenantsSettings;
            if (File.Exists(_tenants))
            {
                using var streamReader = File.OpenRead(_tenants);
                tenantsSettings = await JObject.LoadAsync(streamReader);
            }
            else
            {
                tenantsSettings = [];
            }

            var settings = tenantsSettings[tenant] as JsonObject ?? [];
            foreach (var key in data.Keys)
            {
                if (data[key] is not null)
                {
                    settings[key] = data[key];
                }
                else
                {
                    settings.Remove(key);
                }
            }

            tenantsSettings[tenant] = settings;

            using var streamWriter = File.Create(_tenants);
            await JsonSerializer.SerializeAsync(streamWriter, tenantsSettings, JOptions.Indented);
        }

        public async Task RemoveAsync(string tenant)
        {
            if (File.Exists(_tenants))
            {
                JsonObject tenantsSettings;
                using (var streamReader = File.OpenRead(_tenants))
                {
                    tenantsSettings = await JObject.LoadAsync(streamReader);
                }

                tenantsSettings.Remove(tenant);

                using var streamWriter = File.Create(_tenants);
                await JsonSerializer.SerializeAsync(streamWriter, tenantsSettings, JOptions.Indented);
            }
        }
    }
}
