using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Repository;

public class ConsulFileConfigurationPollerOption : IFileConfigurationPollerOptions
{
    private readonly IInternalConfigurationRepository _internalConfigRepo;
    private readonly IFileConfigurationRepository _fileConfigurationRepository;

    public ConsulFileConfigurationPollerOption(IInternalConfigurationRepository internalConfigurationRepository,
                                               IFileConfigurationRepository fileConfigurationRepository)
    {
        _internalConfigRepo = internalConfigurationRepository;
        _fileConfigurationRepository = fileConfigurationRepository;
    }

    public int Delay() => GetDelay();

    public Task<int> DelayAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(GetDelay());

    private int GetDelay()
    {
        var delay = InMemoryFileConfigurationPollerOptions.DefaultDelayMilliseconds;

        FileConfiguration fileConfig;
        try
        {
            fileConfig = _fileConfigurationRepository.Get();
        }
        catch
        {
            fileConfig = null;
        }

        if (fileConfig?.GlobalConfiguration?.ServiceDiscoveryProvider != null &&
                fileConfig.GlobalConfiguration.ServiceDiscoveryProvider.PollingInterval > 0)
        {
            delay = fileConfig.GlobalConfiguration.ServiceDiscoveryProvider.PollingInterval;
        }
        else
        {
            var internalConfig = _internalConfigRepo.Get();
            if (internalConfig?.ServiceProviderConfiguration != null &&
                internalConfig.ServiceProviderConfiguration.PollingInterval > 0)
            {
                delay = internalConfig.ServiceProviderConfiguration.PollingInterval;
            }
        }

        return delay;
    }
}
