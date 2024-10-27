using Consumer.Services.Implementations;
using Consumer.Services.Interfaces;
using Shared.Extensions;

namespace Consumer;

public static class DependencyInjection
{
    public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddProjectModels(configuration)
            .AddRabbitMq();

        services.AddScoped<IMyService, MyService>();
        
        return services;
    }
}