using Shared.Extensions;

namespace Publisher;

public static class DependencyInjection
{
    public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddProjectModels(configuration)
            .AddRabbitMq();

        return services;
    }
}