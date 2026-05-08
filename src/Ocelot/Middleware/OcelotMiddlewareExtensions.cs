using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Ocelot.Administration;
using Ocelot.Configuration;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Repository;
using Ocelot.Infrastructure.Extensions;
using Ocelot.Logging;
using Ocelot.Responses;
using System.Diagnostics;

namespace Ocelot.Middleware;

public static class OcelotMiddlewareExtensions
{
    public static async Task<IApplicationBuilder> UseOcelot(this IApplicationBuilder builder)
    {
        await builder.UseOcelot(new OcelotPipelineConfiguration());
        return builder;
    }

    public static async Task<IApplicationBuilder> UseOcelot(this IApplicationBuilder builder, Action<OcelotPipelineConfiguration> pipelineConfiguration)
    {
        var config = new OcelotPipelineConfiguration();
        pipelineConfiguration?.Invoke(config);
        return await builder.UseOcelot(config);
    }

    public static async Task<IApplicationBuilder> UseOcelot(this IApplicationBuilder builder, OcelotPipelineConfiguration pipelineConfiguration)
    {
        _ = await CreateConfiguration(builder);

        ConfigureDiagnosticListener(builder);

        return CreateOcelotPipeline(builder, pipelineConfiguration);
    }

    public static Task<IApplicationBuilder> UseOcelot(this IApplicationBuilder app, Action<IApplicationBuilder, OcelotPipelineConfiguration> builderAction)
        => UseOcelot(app, builderAction, new OcelotPipelineConfiguration());

    public static async Task<IApplicationBuilder> UseOcelot(this IApplicationBuilder app, Action<IApplicationBuilder, OcelotPipelineConfiguration> builderAction, OcelotPipelineConfiguration configuration)
    {
        await CreateConfiguration(app);

        ConfigureDiagnosticListener(app);

        builderAction?.Invoke(app, configuration ?? new OcelotPipelineConfiguration());

        app.Properties["analysis.NextMiddlewareName"] = "TransitionToOcelotMiddleware";

        return app;
    }

    private static IApplicationBuilder CreateOcelotPipeline(IApplicationBuilder builder, OcelotPipelineConfiguration pipelineConfiguration)
    {
        builder.BuildOcelotPipeline(pipelineConfiguration);

        builder.Properties["analysis.NextMiddlewareName"] = "TransitionToOcelotMiddleware";

        return builder;
    }

    private static async Task<IInternalConfiguration> CreateConfiguration(IApplicationBuilder builder)
    {
        // make configuration from file system?
        var fileConfig = builder.ApplicationServices.GetService<IOptionsMonitor<FileConfiguration>>();

        // now create the config
        var internalConfigCreator = builder.ApplicationServices.GetService<IInternalConfigurationCreator>();
        var internalConfig = await internalConfigCreator.Create(fileConfig.CurrentValue);

        //Configuration error, throw error message
        if (internalConfig.IsError)
        {
            ThrowToStopOcelotStarting(internalConfig);
        }

        // now save it in memory
        var internalConfigRepo = builder.ApplicationServices.GetService<IInternalConfigurationRepository>();
        internalConfigRepo.AddOrReplace(internalConfig.Data);

        fileConfig.OnChange(async (config) =>
        {
            var newInternalConfig = await internalConfigCreator.Create(config);
            internalConfigRepo.AddOrReplace(newInternalConfig.Data);
        });

        var adminPath = builder.ApplicationServices.GetService<IAdministrationPath>();

        var configurations = builder.ApplicationServices.GetServices<OcelotMiddlewareConfigurationDelegate>();

        foreach (var conf in configurations)
        {
            await conf(builder);
        }

        if (adminPath != null) // Administration API is in use
        {
            var fileConfigSetter = builder.ApplicationServices.GetService<IFileConfigurationSetter>();

            // Internally throws ConfigurationRepositoryException to stop Ocelot starting
            await fileConfigSetter.SetAsync(fileConfig.CurrentValue, CancellationToken.None);
        }

        return GetOcelotConfigAndReturn(internalConfigRepo);
    }

    private static IInternalConfiguration GetOcelotConfigAndReturn(IInternalConfigurationRepository provider)
    {
        var ocelotConfiguration = provider.Get();

        if (ocelotConfiguration == null)
        {
            throw new Exception("Unable to start Ocelot, configuration returned null");
        }

        return ocelotConfiguration;
    }

    private static void ThrowToStopOcelotStarting(Response config)
    {
        throw new Exception($"Unable to start Ocelot, errors are:{config.Errors.ToErrorString(true, true)}");
    }

    private static void ConfigureDiagnosticListener(IApplicationBuilder builder)
    {
        _ = builder.ApplicationServices.GetService<IWebHostEnvironment>();
        var listener = builder.ApplicationServices.GetService<OcelotDiagnosticListener>();
        var diagnosticListener = builder.ApplicationServices.GetService<DiagnosticListener>();
        diagnosticListener.SubscribeWithAdapter(listener);
    }
}
