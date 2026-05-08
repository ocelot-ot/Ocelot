using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Repository;
using Ocelot.Middleware;

namespace Ocelot.Provider.Consul;

public static class ConsulMiddlewareConfigurationProvider
{
    public static OcelotMiddlewareConfigurationDelegate Get { get; } = GetAsync;

    private static async Task GetAsync(IApplicationBuilder builder)
    {
        var fileConfigRepo = builder.ApplicationServices.GetService<IFileConfigurationRepository>();
        var fileConfig = builder.ApplicationServices.GetService<IOptionsMonitor<FileConfiguration>>();
        var internalConfigCreator = builder.ApplicationServices.GetService<IInternalConfigurationCreator>();
        var internalConfigRepo = builder.ApplicationServices.GetService<IInternalConfigurationRepository>();

        if (UsingConsul(fileConfigRepo))
        {
            await SetFileConfigInConsul(builder, fileConfigRepo, fileConfig, internalConfigCreator, internalConfigRepo);
        }
    }

    private static bool UsingConsul(IFileConfigurationRepository fileConfigRepo)
        => fileConfigRepo.GetType() == typeof(ConsulFileConfigurationRepository);

    private static async Task SetFileConfigInConsul(IApplicationBuilder builder,
        IFileConfigurationRepository fileConfigRepo, IOptionsMonitor<FileConfiguration> fileConfig,
        IInternalConfigurationCreator internalConfigCreator, IInternalConfigurationRepository internalConfigRepo)
    {
        // Get the config from Consul
        FileConfiguration fileConfigFromConsul;
        try
        {
            fileConfigFromConsul = await fileConfigRepo.GetAsync();
        }
        catch (Exception ex)
        {
            throw new Exception($"Unable to start Ocelot, error getting config from Consul: {ex.Message}", ex);
        }

        if (fileConfigFromConsul == null)
        {
            // there was no config in Consul - set the file config in Consul
            await fileConfigRepo.SetAsync(fileConfig.CurrentValue);
        }
        else
        {
            // Create the internal config from Consul data
            var internalConfig = await internalConfigCreator.Create(fileConfigFromConsul);
            if (internalConfig.IsError)
            {
                throw new Exception($"Unable to start Ocelot, errors are:{string.Join(',', internalConfig.Errors.Select(x => x.Message))}");
            }

            internalConfigRepo.AddOrReplace(internalConfig.Data);
        }
    }
}
