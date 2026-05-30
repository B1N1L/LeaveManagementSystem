using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Messaging;
using LeaveService.Services;

namespace LeaveService.Messaging;

// Shape of message received from User Service
public class UserCreatedMessage
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class UserCreatedConsumer : BackgroundService
{
    private readonly RabbitMQConnectionHelper _connectionHelper;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<UserCreatedConsumer> _logger;
    private IChannel? _channel;

    public UserCreatedConsumer(
        RabbitMQConnectionHelper connectionHelper,
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<UserCreatedConsumer> logger)
    {
        _connectionHelper = connectionHelper;
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var queueName = _configuration["RabbitMQ:UserCreatedQueue"]!;

        // Get channel and declare queue
        _channel = await _connectionHelper.GetChannelAsync(queueName);

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (sender, eventArgs) =>
        {
            try
            {
                var body = eventArgs.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);
                var message = JsonSerializer.Deserialize<UserCreatedMessage>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (message == null)
                {
                    _logger.LogWarning("Received null UserCreated message");
                    await _channel.BasicNackAsync(eventArgs.DeliveryTag, false, false);
                    return;
                }

                _logger.LogInformation(
                    "Received UserCreated event for user {UserId} ({Email})",
                    message.UserId, message.Email);

                // Use a scope because LeaveManagementService is Scoped
                // but this consumer is a Singleton background service
                using var scope = _scopeFactory.CreateScope();
                var leaveService = scope.ServiceProvider
                    .GetRequiredService<LeaveManagementService>();

                await leaveService.InitializeLeaveBalanceAsync(message.UserId);

                // Acknowledge message — tells RabbitMQ we processed it successfully
                await _channel.BasicAckAsync(eventArgs.DeliveryTag, false);

                _logger.LogInformation(
                    "Leave balance initialized for user {UserId}",
                    message.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error processing UserCreated message");

                // Negative acknowledge — message goes back to queue
                await _channel.BasicNackAsync(eventArgs.DeliveryTag, false, true);
            }
        };

        // Start consuming
        await _channel.BasicConsumeAsync(
            queue: queueName,
            autoAck: false,     // Manual acknowledgement — safer
            consumer: consumer);

        // Keep running until app shuts down
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    public override void Dispose()
    {
        _channel?.CloseAsync();
        base.Dispose();
    }
}