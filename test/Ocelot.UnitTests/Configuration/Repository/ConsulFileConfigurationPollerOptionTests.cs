using Ocelot.Configuration;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Repository;
using Ocelot.Provider.Consul;

namespace Ocelot.UnitTests.Configuration.Repository;

public class ConsulFileConfigurationPollerOptionTests
{
    private readonly Mock<IInternalConfigurationRepository> _mockInternalConfigRepo = new();
    private readonly Mock<IFileConfigurationRepository> _mockFileConfigurationRepository = new();
    private readonly ConsulFileConfigurationPollerOption _sut;

    public ConsulFileConfigurationPollerOptionTests()
    {
        _sut = new(
            _mockInternalConfigRepo.Object,
            _mockFileConfigurationRepository.Object);
    }

    [Fact]
    public void Constructor_ShouldSetDependencies()
    {
        // Arrange & Act
        var result = _sut;

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void Delay_ShouldReturnDefaultValue_WhenFileConfigurationIsNull()
    {
        // Arrange
        _mockFileConfigurationRepository
            .Setup(x => x.Get())
            .Returns((FileConfiguration)null);

        _mockInternalConfigRepo
            .Setup(x => x.Get())
            .Returns((IInternalConfiguration)null);

        // Act
        var delay = _sut.Delay();

        // Assert
        Assert.Equal(InMemoryFileConfigurationPollerOptions.DefaultDelayMilliseconds, delay);
    }

    [Fact]
    public void Delay_ShouldReturnFileConfigPollingInterval_WhenFileConfigHasValidPollingInterval()
    {
        // Arrange
        const int expectedDelay = 5000;
        var fileConfiguration = new FileConfiguration
        {
            GlobalConfiguration = new()
            {
                ServiceDiscoveryProvider = new()
                {
                    PollingInterval = expectedDelay
                }
            }
        };

        _mockFileConfigurationRepository
            .Setup(x => x.Get())
            .Returns(fileConfiguration);

        // Act
        var delay = _sut.Delay();

        // Assert
        Assert.Equal(expectedDelay, delay);
    }

    [Fact]
    public void Delay_ShouldReturnDefaultValue_WhenFileConfigPollingIntervalIsZero()
    {
        // Arrange
        var fileConfiguration = new FileConfiguration
        {
            GlobalConfiguration = new()
            {
                ServiceDiscoveryProvider = new()
                {
                    PollingInterval = 0
                }
            }
        };

        _mockFileConfigurationRepository
            .Setup(x => x.Get())
            .Returns(fileConfiguration);

        _mockInternalConfigRepo
            .Setup(x => x.Get())
            .Returns((IInternalConfiguration)null);

        // Act
        var delay = _sut.Delay();

        // Assert
        Assert.Equal(InMemoryFileConfigurationPollerOptions.DefaultDelayMilliseconds, delay);
    }

    [Fact]
    public void Delay_ShouldReturnDefaultValue_WhenFileConfigThrows()
    {
        // Arrange
        _mockFileConfigurationRepository
            .Setup(x => x.Get())
            .Throws(new Exception("Error"));

        _mockInternalConfigRepo
            .Setup(x => x.Get())
            .Returns((IInternalConfiguration)null);

        // Act
        var delay = _sut.Delay();

        // Assert
        Assert.Equal(InMemoryFileConfigurationPollerOptions.DefaultDelayMilliseconds, delay);
    }

    [Fact]
    public void Delay_ShouldReturnDefaultValue_WhenFileConfigServiceDiscoveryProviderIsNull()
    {
        // Arrange
        var fileConfiguration = new FileConfiguration
        {
            GlobalConfiguration = new()
            {
                ServiceDiscoveryProvider = null
            }
        };

        _mockFileConfigurationRepository
            .Setup(x => x.Get())
            .Returns(fileConfiguration);

        _mockInternalConfigRepo
            .Setup(x => x.Get())
            .Returns((IInternalConfiguration)null);

        // Act
        var delay = _sut.Delay();

        // Assert
        Assert.Equal(InMemoryFileConfigurationPollerOptions.DefaultDelayMilliseconds, delay);
    }

    [Fact]
    public void Delay_ShouldReturnInternalConfigPollingInterval_WhenFileConfigFailsButInternalConfigIsValid()
    {
        // Arrange
        const int expectedDelay = 3000;
        _mockFileConfigurationRepository
            .Setup(x => x.Get())
            .Returns((FileConfiguration)null);

        var internalConfiguration = new InternalConfiguration
        {
            ServiceProviderConfiguration = new()
            {
                PollingInterval = expectedDelay,
            }
        };
        _mockInternalConfigRepo
            .Setup(x => x.Get())
            .Returns(internalConfiguration);

        // Act
        var delay = _sut.Delay();

        // Assert
        Assert.Equal(expectedDelay, delay);
    }

    [Fact]
    public void Delay_ShouldReturnDefaultValue_WhenInternalConfigPollingIntervalIsZero()
    {
        // Arrange
        _mockFileConfigurationRepository
            .Setup(x => x.Get())
            .Returns((FileConfiguration)null);

        var internalConfiguration = new InternalConfiguration
        {
            ServiceProviderConfiguration = new()
            {
                PollingInterval = 0,
            }
        };
        _mockInternalConfigRepo
            .Setup(x => x.Get())
            .Returns(internalConfiguration);

        // Act
        var delay = _sut.Delay();

        // Assert
        Assert.Equal(InMemoryFileConfigurationPollerOptions.DefaultDelayMilliseconds, delay);
    }

    [Fact]
    public void Delay_ShouldReturnDefaultValue_WhenInternalConfigIsNull()
    {
        // Arrange
        _mockFileConfigurationRepository
            .Setup(x => x.Get())
            .Returns((FileConfiguration)null);

        _mockInternalConfigRepo
            .Setup(x => x.Get())
            .Returns((IInternalConfiguration)null);

        // Act
        var delay = _sut.Delay();

        // Assert
        Assert.Equal(InMemoryFileConfigurationPollerOptions.DefaultDelayMilliseconds, delay);
    }

    [Fact]
    public void Delay_ShouldReturnDefaultValue_WhenInternalConfigServiceProviderConfigurationIsNull()
    {
        // Arrange
        _mockFileConfigurationRepository
            .Setup(x => x.Get())
            .Returns((FileConfiguration)null);

        var internalConfiguration = new InternalConfiguration
        {
            ServiceProviderConfiguration = null
        };
        _mockInternalConfigRepo
            .Setup(x => x.Get())
            .Returns(internalConfiguration);

        // Act
        var delay = _sut.Delay();

        // Assert
        Assert.Equal(InMemoryFileConfigurationPollerOptions.DefaultDelayMilliseconds, delay);
    }

    [Fact]
    public void Delay_ShouldPreferFileConfigOverInternalConfig_WhenBothHaveValidPollingIntervals()
    {
        // Arrange
        const int fileConfigDelay = 5000;
        const int internalConfigDelay = 3000;

        var fileConfiguration = new FileConfiguration
        {
            GlobalConfiguration = new()
            {
                ServiceDiscoveryProvider = new()
                {
                    PollingInterval = fileConfigDelay,
                }
            }
        };
        _mockFileConfigurationRepository
            .Setup(x => x.Get())
            .Returns(fileConfiguration);

        var internalConfiguration = new InternalConfiguration
        {
            ServiceProviderConfiguration = new ServiceProviderConfiguration
            {
                PollingInterval = internalConfigDelay
            }
        };
        _mockInternalConfigRepo
            .Setup(x => x.Get())
            .Returns(internalConfiguration);

        // Act
        var delay = _sut.Delay();

        // Assert
        Assert.Equal(fileConfigDelay, delay);
    }

    [Fact]
    public async Task DelayAsync_ShouldReturnDelay()
    {
        // Arrange
        const int expectedDelay = 5000;
        var fileConfiguration = new FileConfiguration
        {
            GlobalConfiguration = new()
            {
                ServiceDiscoveryProvider = new()
                {
                    PollingInterval = expectedDelay
                }
            }
        };
        _mockFileConfigurationRepository
            .Setup(x => x.Get())
            .Returns(fileConfiguration);

        // Act
        var delay = await _sut.DelayAsync();

        // Assert
        Assert.Equal(expectedDelay, delay);
    }

    [Theory]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(5000)]
    [InlineData(10000)]
    public void Delay_ShouldReturnValidPollingInterval_WithVariousValues(int pollingInterval)
    {
        // Arrange
        var fileConfiguration = new FileConfiguration
        {
            GlobalConfiguration = new()
            {
                ServiceDiscoveryProvider = new()
                {
                    PollingInterval = pollingInterval
                }
            }
        };
        _mockFileConfigurationRepository
            .Setup(x => x.Get())
            .Returns(fileConfiguration);

        // Act
        var delay = _sut.Delay();

        // Assert
        Assert.Equal(pollingInterval, delay);
    }
}
