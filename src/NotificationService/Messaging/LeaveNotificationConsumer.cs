using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Messaging;
using NotificationService.Models;
using NotificationService.Services;

namespace NotificationService.Messaging;

// Message shape received from Leave Service
public class LeaveNotificationMessage
{
    public string EventType { get; set; } = string.Empty;
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string LeaveType { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalDays { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime ActedOn { get; set; }
}

public class LeaveNotificationConsumer : BackgroundService
{
    private readonly RabbitMQConnectionHelper _connectionHelper;
    private readonly NotificationStore _store;
    private readonly IConfiguration _configuration;
    private readonly ILogger<LeaveNotificationConsumer> _logger;
    private IChannel? _channel;

    public LeaveNotificationConsumer(
        RabbitMQConnectionHelper connectionHelper,
        NotificationStore store,
        IConfiguration configuration,
        ILogger<LeaveNotificationConsumer> logger)
    {
        _connectionHelper = connectionHelper;
        _store = store;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var queueName = _configuration["RabbitMQ:QueueName"]!;
        _channel = await _connectionHelper.GetChannelAsync(queueName);

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (sender, eventArgs) =>
        {
            try
            {
                var body = eventArgs.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);

                var message = JsonSerializer.Deserialize<LeaveNotificationMessage>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (message == null)
                {
                    _logger.LogWarning("Received null leave notification message");
                    await _channel.BasicNackAsync(eventArgs.DeliveryTag, false, false);
                    return;
                }

                // Store in memory
                var notification = new Notification
                {
                    EventType = message.EventType,
                    EmployeeId = message.EmployeeId,
                    EmployeeName = message.EmployeeName,
                    LeaveType = message.LeaveType,
                    StartDate = message.StartDate,
                    EndDate = message.EndDate,
                    TotalDays = message.TotalDays,
                    RejectionReason = message.RejectionReason,
                    ActedOn = message.ActedOn,
                    ReceivedAt = DateTime.UtcNow
                };

                _store.Add(notification);

                // Log based on event type — this is the assignment requirement
                if (message.EventType == "LeaveApproved")
                {
                    _logger.LogInformation(
                        "[NOTIFICATION] LEAVE APPROVED — " +
                        "Employee: {EmployeeName} (ID: {EmployeeId}) | " +
                        "Type: {LeaveType} | " +
                        "From: {StartDate:yyyy-MM-dd} To: {EndDate:yyyy-MM-dd} | " +
                        "Days: {TotalDays} | " +
                        "Approved On: {ActedOn:yyyy-MM-dd HH:mm:ss}",
                        message.EmployeeName, message.EmployeeId,
                        message.LeaveType,
                        message.StartDate, message.EndDate,
                        message.TotalDays,
                        message.ActedOn);
                }
                else if (message.EventType == "LeaveRejected")
                {
                    _logger.LogInformation(
                        "[NOTIFICATION] LEAVE REJECTED — " +
                        "Employee: {EmployeeName} (ID: {EmployeeId}) | " +
                        "Type: {LeaveType} | " +
                        "From: {StartDate:yyyy-MM-dd} To: {EndDate:yyyy-MM-dd} | " +
                        "Days: {TotalDays} | " +
                        "Reason: {RejectionReason} | " +
                        "Rejected On: {ActedOn:yyyy-MM-dd HH:mm:ss}",
                        message.EmployeeName, message.EmployeeId,
                        message.LeaveType,
                        message.StartDate, message.EndDate,
                        message.TotalDays,
                        message.RejectionReason,
                        message.ActedOn);
                }

                await _channel.BasicAckAsync(eventArgs.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing leave notification");
                await _channel.BasicNackAsync(eventArgs.DeliveryTag, false, true);
            }
        };

        await _channel.BasicConsumeAsync(
            queue: queueName,
            autoAck: false,
            consumer: consumer);

        while (!stoppingToken.IsCancellationRequested)
            await Task.Delay(1000, stoppingToken);
    }

    public override void Dispose()
    {
        _channel?.CloseAsync();
        base.Dispose();
    }
}