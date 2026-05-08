using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Ocelot.Cache;
using Ocelot.Configuration;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Repository;
using Ocelot.Logging;
using Ocelot.Provider.Consul.Interfaces;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ocelot.Provider.Consul;

public class ConsulFileConfigurationRepository : IFileConfigurationRepository
{
    private readonly IOcelotCache<FileConfiguration> _cache;
    private readonly string _configurationKey;
    private readonly IConsulClient _consul;
    private readonly IOcelotLogger _logger;

    public ConsulFileConfigurationRepository(
        IOptions<FileConfiguration> fileConfiguration,
        IOcelotCache<FileConfiguration> cache,
        IConsulClientFactory factory,
        IOcelotLoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<ConsulFileConfigurationRepository>();
        _cache = cache;

        var provider = fileConfiguration.Value.GlobalConfiguration.ServiceDiscoveryProvider;
        _configurationKey = string.IsNullOrWhiteSpace(provider.ConfigurationKey)
            ? nameof(InternalConfiguration)
            : provider.ConfigurationKey;

        var config = new ConsulRegistryConfiguration(provider.Scheme, provider.Host,
            provider.Port, _configurationKey, provider.Token);
        _consul = factory.Get(config);
    }

    public FileConfiguration Get()
    {
        var config = _cache.Get(_configurationKey, _configurationKey);
        if (config != null)
            return config;

        var queryResult = _consul.KV.Get(_configurationKey).GetAwaiter().GetResult();
        if (queryResult.Response == null)
            return null;

        var bytes = queryResult.Response.Value;
        var json = Encoding.UTF8.GetString(bytes);
        return JsonConvert.DeserializeObject<FileConfiguration>(json);
    }

    public async Task<FileConfiguration> GetAsync(CancellationToken cancellationToken = default)
    {
        var config = _cache.Get(_configurationKey, _configurationKey);
        if (config != null)
            return config;

        var queryResult = await _consul.KV.Get(_configurationKey);
        if (queryResult.Response == null)
            return null;

        var bytes = queryResult.Response.Value;
        var json = Encoding.UTF8.GetString(bytes);
        return JsonConvert.DeserializeObject<FileConfiguration>(json);
    }

    public void Set(FileConfiguration configuration)
    {
        var json = JsonConvert.SerializeObject(configuration, Formatting.Indented);
        var bytes = Encoding.UTF8.GetBytes(json);
        var kvPair = new KVPair(_configurationKey) { Value = bytes };

        var result = _consul.KV.Put(kvPair).GetAwaiter().GetResult();
        if (result.Response)
        {
            _cache.AddOrUpdate(_configurationKey, configuration, _configurationKey, TimeSpan.FromSeconds(3));
        }
    }

    public async Task SetAsync(FileConfiguration configuration, CancellationToken cancellationToken = default)
    {
        var json = JsonConvert.SerializeObject(configuration, Formatting.Indented);
        var bytes = Encoding.UTF8.GetBytes(json);
        var kvPair = new KVPair(_configurationKey) { Value = bytes };

        var result = await _consul.KV.Put(kvPair);
        if (result.Response)
        {
            _cache.AddOrUpdate(_configurationKey, configuration, _configurationKey, TimeSpan.FromSeconds(3));
        }
    }
}
