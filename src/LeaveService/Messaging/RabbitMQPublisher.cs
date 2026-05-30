using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using Shared.Messaging;
using LeaveService.DTOs;

namespace LeaveService.Messaging;

public class RabbitMQPublisher : IDisposable
{
    private readonly RabbitMQConnectionHelper _connectionHelper;
    private readonly string _queueName;
    private readonly ILogger<RabbitMQPublisher> _logger;

    public RabbitMQPublisher(
        RabbitMQConnectionHelper connectionHelper,
        IConfiguration configuration,
        ILogger<RabbitMQPublisher> logger)
    {
        _connectionHelper = connectionHelper;
        _queueName = configuration["RabbitMQ:QueueName"]!;
        _logger = logger;
    }

    public async Task PublishAsync(LeaveNotificationMessage message)
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
                "Published {EventType} for employee {EmployeeId}",
                message.EventType, message.EmployeeId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish leave notification");
            throw;
        }
    }

    public void Dispose() => _connectionHelper.Dispose();
}