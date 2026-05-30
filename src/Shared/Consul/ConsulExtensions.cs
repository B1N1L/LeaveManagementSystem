using Consul;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Shared.Consul;

public static class ConsulExtensions
{
    public static IServiceCollection AddConsulRegistration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IConsulClient>(_ => new ConsulClient(config =>
        {
            config.Address = new Uri(configuration["Consul:Host"]!);
        }));

        services.AddHostedService<ConsulRegistrationService>();

        return services;
    }
}