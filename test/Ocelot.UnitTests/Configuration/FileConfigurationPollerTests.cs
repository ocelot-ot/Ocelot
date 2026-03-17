using Ocelot.Configuration;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Repository;
using Ocelot.Logging;
using Ocelot.Responses;
using Ocelot.UnitTests.Responder;

namespace Ocelot.UnitTests.Configuration;

public sealed class FileConfigurationPollerTests : UnitTest, IDisposable
{
    private readonly FileConfigurationPoller _poller;
    private readonly Mock<IOcelotLoggerFactory> _factory;
    private readonly Mock<IFileConfigurationRepository> _repo;
    private readonly FileConfiguration _initialFileConfig;
    private readonly Mock<IFileConfigurationPollerOptions> _config;
    private readonly Mock<IInternalConfigurationRepository> _internalConfigRepo;
    private readonly Mock<IInternalConfigurationCreator> _internalConfigCreator;
    private readonly Mock<IInternalConfiguration> _internalConfig;

    public FileConfigurationPollerTests()
    {
        var logger = new Mock<IOcelotLogger>();
        _factory = new Mock<IOcelotLoggerFactory>();
        _factory.Setup(x => x.CreateLogger<FileConfigurationPoller>()).Returns(logger.Object);
        _repo = new Mock<IFileConfigurationRepository>();
        _initialFileConfig = new FileConfiguration();
        _config = new Mock<IFileConfigurationPollerOptions>();
        _repo.Setup(x => x.Get()).ReturnsAsync(new OkResponse<FileConfiguration>(_initialFileConfig));
        _config.Setup(x => x.Delay).Returns(100);
        _internalConfig = new Mock<IInternalConfiguration>();
        _internalConfigRepo = new Mock<IInternalConfigurationRepository>();
        _internalConfigCreator = new Mock<IInternalConfigurationCreator>();
        _internalConfigCreator.Setup(x => x.Create(It.IsAny<FileConfiguration>())).ReturnsAsync(new OkResponse<IInternalConfiguration>(_internalConfig.Object));
        _poller = new FileConfigurationPoller(_factory.Object, _repo.Object, _config.Object, _internalConfigRepo.Object, _internalConfigCreator.Object);
    }

    [Fact]
    public void Should_start_and_poll_initial_configuration()
    {
        // Arrange, Act
        _poller.StartAsync(CancellationToken.None);

        // Assert
        ThenTheSetterIsCalled(_initialFileConfig, 1);
    }

    [Fact]
    public void Should_call_setter_when_gets_new_config()
    {
        // Arrange
        var newConfig = new FileConfiguration
        {
            Routes = new List<FileRoute>
            {
                new()
                {
                    DownstreamHostAndPorts = new List<FileHostAndPort>
                    {
                        new("test", 80),
                    },
                },
            },
        };

        // Act
        _poller.StartAsync(CancellationToken.None);

        // Assert
        WhenTheConfigIsChanged(newConfig, 0);
        ThenTheSetterIsCalled(newConfig, 1);
    }

    [Fact]
    public void Should_not_call_setter_when_configuration_is_not_changed()
    {
        // Arrange, Act
        _poller.StartAsync(CancellationToken.None);

        // Assert
        ThenTheSetterIsCalled(_initialFileConfig, 1);
        ThenTheConfigIsNotAddedMoreThan(1);
    }

    [Fact]
    public void Should_not_poll_if_already_polling()
    {
        // Arrange
        var newConfig = new FileConfiguration
        {
            Routes = new List<FileRoute>
            {
                new()
                {
                    DownstreamHostAndPorts = new List<FileHostAndPort>
                    {
                        new("test", 80),
                    },
                },
            },
        };

        // Act
        _poller.StartAsync(CancellationToken.None);

        // Assert
        WhenTheConfigIsChanged(newConfig, 250);
        ThenTheSetterIsCalled(newConfig, 1);
    }

    [Fact]
    public void Should_do_nothing_if_call_to_provider_fails()
    {
        // Arrange, Act
        WhenProviderErrors();
        _poller.StartAsync(CancellationToken.None);

        // Assert
        ThenTheSetterIsNotCalled();
    }

    [Fact]
    public void Should_not_add_to_internal_repo_if_internal_configuration_creation_fails()
    {
        // Arrange
        var newConfig = new FileConfiguration
        {
            Routes = new List<FileRoute>
            {
                new()
                {
                    DownstreamHostAndPorts = new List<FileHostAndPort>
                    {
                        new("test", 80),
                    },
                },
            },
        };

        _internalConfigCreator
            .Setup(x => x.Create(It.IsAny<FileConfiguration>()))
            .ReturnsAsync(new ErrorResponse<IInternalConfiguration>(new AnyError()));
        _repo.Setup(x => x.Get()).ReturnsAsync(new OkResponse<FileConfiguration>(newConfig));

        // Act
        _poller.StartAsync(CancellationToken.None);

        // Assert
        ThenTheCreatorIsCalled(newConfig, 1);
        ThenTheConfigIsNotAdded();
    }

    [Fact]
    public void Should_stop_polling_when_stopped()
    {
        // Arrange
        var newConfig = new FileConfiguration
        {
            Routes = new List<FileRoute>
            {
                new()
                {
                    DownstreamHostAndPorts = new List<FileHostAndPort>
                    {
                        new("test", 80),
                    },
                },
            },
        };

        // Act
        _poller.StartAsync(CancellationToken.None);
        ThenTheSetterIsCalled(_initialFileConfig, 1);
        _poller.StopAsync(CancellationToken.None);
        Thread.Sleep(300);
        WhenTheConfigIsChanged(newConfig, 0);
        Thread.Sleep(300);

        // Assert
        ThenTheConfigIsNotAddedMoreThan(1);
        ThenTheCreatorIsCalled(1);
    }

    [Fact]
    public void Should_dispose_cleanly_without_starting()
    {
        // Arrange, Act, Assert
        _poller.Dispose(); // when poller is disposed
    }

    private void WhenProviderErrors()
    {
        _repo
            .Setup(x => x.Get())
            .ReturnsAsync(new ErrorResponse<FileConfiguration>(new AnyError()));
    }

    private void WhenTheConfigIsChanged(FileConfiguration newConfig, int delay)
    {
        _repo
            .Setup(x => x.Get())
            .Callback(() => Thread.Sleep(delay))
            .ReturnsAsync(new OkResponse<FileConfiguration>(newConfig));
    }

    private void ThenTheSetterIsCalled(FileConfiguration fileConfig, int times)
    {
        var result = Wait.For(4_000).Until(() =>
        {
            try
            {
                _internalConfigRepo.Verify(x => x.AddOrReplace(_internalConfig.Object), Times.Exactly(times));
                _internalConfigCreator.Verify(x => x.Create(fileConfig), Times.Exactly(times));
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        });
        result.ShouldBeTrue();
    }

    private void ThenTheSetterIsNotCalled()
    {
        var result = Wait.For(4_000).Until(() =>
        {
            try
            {
                _internalConfigRepo.Verify(x => x.AddOrReplace(It.IsAny<IInternalConfiguration>()), Times.Never);
                _internalConfigCreator.Verify(x => x.Create(It.IsAny<FileConfiguration>()), Times.Never);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        });
        result.ShouldBeTrue();
    }

    private void ThenTheCreatorIsCalled(FileConfiguration fileConfig, int times)
    {
        var result = Wait.For(4_000).Until(() =>
        {
            try
            {
                _internalConfigCreator.Verify(x => x.Create(fileConfig), Times.Exactly(times));
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        });
        result.ShouldBeTrue();
    }

    private void ThenTheCreatorIsCalled(int times)
    {
        var result = Wait.For(4_000).Until(() =>
        {
            try
            {
                _internalConfigCreator.Verify(x => x.Create(It.IsAny<FileConfiguration>()), Times.Exactly(times));
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        });
        result.ShouldBeTrue();
    }

    private void ThenTheConfigIsNotAdded()
    {
        var result = Wait.For(4_000).Until(() =>
        {
            try
            {
                _internalConfigRepo.Verify(x => x.AddOrReplace(It.IsAny<IInternalConfiguration>()), Times.Never);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        });
        result.ShouldBeTrue();
    }

    private void ThenTheConfigIsNotAddedMoreThan(int times)
    {
        var result = Wait.For(4_000).Until(() =>
        {
            try
            {
                _internalConfigRepo.Verify(x => x.AddOrReplace(_internalConfig.Object), Times.Exactly(times));
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        });
        result.ShouldBeTrue();
    }

    public void Dispose()
    {
        _poller.Dispose();
    }
}
