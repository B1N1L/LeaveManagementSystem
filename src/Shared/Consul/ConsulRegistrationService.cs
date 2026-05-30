using Consul;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Shared.Consul;

public class ConsulRegistrationService : IHostedLifecycleService
{
    private readonly IConsulClient _consulClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ConsulRegistrationService> _logger;
    private string _registrationId = string.Empty;

    public ConsulRegistrationService(
        IConsulClient consulClient,
        IConfiguration configuration,
        ILogger<ConsulRegistrationService> logger)
    {
        _consulClient = consulClient;
        _configuration = configuration;
        _logger = logger;
    }

    // Runs AFTER Kestrel is fully started and accepting requests
    public async Task StartedAsync(CancellationToken cancellationToken)
    {
        var serviceName = _configuration["Consul:ServiceName"]!;
        var servicePort = int.Parse(_configuration["Consul:ServicePort"]!);
        var healthCheckHost = _configuration["Consul:HealthCheckHost"]
            ?? "host.docker.internal";
        _registrationId = $"{serviceName}-{servicePort}";

        var registration = new AgentServiceRegistration
        {
            ID = _registrationId,
            Name = serviceName,
            Address = "host.docker.internal",
            Port = servicePort,
            Tags = new[] { "api" },
            Check = new AgentServiceCheck
            {
                HTTP = $"http://{healthCheckHost}:{servicePort}/health",
                Interval = TimeSpan.FromSeconds(10),
                Timeout = TimeSpan.FromSeconds(5),
                DeregisterCriticalServiceAfter = TimeSpan.FromMinutes(2)
            }
        };

        await _consulClient.Agent.ServiceRegister(registration, cancellationToken);

        _logger.LogInformation(
            "Registered {ServiceName} with Consul at port {Port}",
            serviceName, servicePort);
    }

    // Required interface methods — not needed but must be implemented
    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StartingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    // Deregister on shutdown
    public async Task StoppedAsync(CancellationToken cancellationToken)
    {
        await _consulClient.Agent.ServiceDeregister(_registrationId, cancellationToken);
        _logger.LogInformation(
            "Deregistered {RegistrationId} from Consul", _registrationId);
    }
}