using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using Shared.Common.Models;
using Shared.Services;

namespace Shared.Extensions;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddProjectModels(this IServiceCollection services, IConfiguration configuration)
    {
        var projectSetting = configuration.GetSection(ProjectSetting.SectionName).Get<ProjectSetting>();
        ArgumentNullException.ThrowIfNull(projectSetting);

        services.AddSingleton(projectSetting);

        return services;
    }

    public static IServiceCollection AddRabbitMq(this IServiceCollection services)
    {
        var projectSetting = services.BuildServiceProvider().GetRequiredService<ProjectSetting>();
        services.AddSingleton<IConnectionFactory>(new ConnectionFactory
        {
            HostName = projectSetting.RabbitMq.Host,
            Port = projectSetting.RabbitMq.Port,
            UserName = projectSetting.RabbitMq.User,
            Password = projectSetting.RabbitMq.Password
        });
        services.AddSingleton<RabbitMqConnectionService>();

        return services;
    }
}