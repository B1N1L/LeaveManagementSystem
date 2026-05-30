using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;

namespace LeaveService.Messaging;

public class RabbitMQHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;

    public RabbitMQHealthCheck(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMQ:Host"]!,
                Port = int.Parse(_configuration["RabbitMQ:Port"]!),
                UserName = _configuration["RabbitMQ:Username"]!,
                Password = _configuration["RabbitMQ:Password"]!
            };

            await using var connection = await factory.CreateConnectionAsync(cancellationToken);

            return connection.IsOpen
                ? HealthCheckResult.Healthy("RabbitMQ connection is healthy.")
                : HealthCheckResult.Unhealthy("RabbitMQ connection is closed.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "RabbitMQ is unreachable.", ex);
        }
    }
}