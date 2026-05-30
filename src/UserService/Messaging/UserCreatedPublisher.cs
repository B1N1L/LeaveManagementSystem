using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using Shared.Messaging;

namespace UserService.Messaging;

public class UserCreatedMessage
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class UserCreatedPublisher : IDisposable
{
    private readonly RabbitMQConnectionHelper _connectionHelper;
    private readonly string _queueName;
    private readonly ILogger<UserCreatedPublisher> _logger;

    public UserCreatedPublisher(
        RabbitMQConnectionHelper connectionHelper,
        IConfiguration configuration,
        ILogger<UserCreatedPublisher> logger)
    {
        _connectionHelper = connectionHelper;
        _queueName = configuration["RabbitMQ:UserCreatedQueue"]!;
        _logger = logger;
    }

    public async Task PublishAsync(UserCreatedMessage message)
    {
        try
        {
            var channel = await _connectionHelper.GetChannelAsync(_queueName);
            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            var properties = new BasicProperties
            {
                Persistent = true
            };

            await channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: _queueName,
                mandatory: false,
                basicProperties: properties,
                body: body
            );

            _logger.LogInformation(
                "Published UserCreated event for {Email}",
                message.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish UserCreated event");
            throw;
        }
    }

    public void Dispose() => _connectionHelper.Dispose();
}