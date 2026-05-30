using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Shared.Messaging;

public class RabbitMQConnectionHelper : IDisposable
{
    private IConnection? _connection;
    private IChannel? _channel;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RabbitMQConnectionHelper> _logger;

    public RabbitMQConnectionHelper(
        IConfiguration configuration,
        ILogger<RabbitMQConnectionHelper> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<IChannel> GetChannelAsync(string queueName)
    {
        if (_connection == null || !_connection.IsOpen)
        {
            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMQ:Host"]!,
                Port = int.Parse(_configuration["RabbitMQ:Port"]!),
                UserName = _configuration["RabbitMQ:Username"]!,
                Password = _configuration["RabbitMQ:Password"]!
            };

            _connection = await factory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();

            _logger.LogInformation(
                "RabbitMQ connection established for queue: {QueueName}",
                queueName);
        }

        // Declare the queue — safe to call multiple times
        await _channel!.QueueDeclareAsync(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false
        );

        return _channel;
    }

    public void Dispose()
    {
        _channel?.CloseAsync();
        _connection?.CloseAsync();
    }
}