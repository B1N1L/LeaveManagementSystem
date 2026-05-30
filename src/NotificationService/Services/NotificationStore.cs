using System.Collections.Concurrent;
using NotificationService.Models;

namespace NotificationService.Services;

// Singleton service — lives for the entire app lifetime
public class NotificationStore
{
    // Thread-safe collection — safe to read/write from multiple threads
    private readonly ConcurrentQueue<Notification> _notifications = new();

    // Keep max 200 notifications in memory
    private const int MaxNotifications = 200;

    public void Add(Notification notification)
    {
        _notifications.Enqueue(notification);

        // Remove oldest if we exceed the limit
        while (_notifications.Count > MaxNotifications)
            _notifications.TryDequeue(out _);
    }

    public IReadOnlyList<Notification> GetAll() =>
        _notifications.OrderByDescending(n => n.ReceivedAt).ToList();

    public IReadOnlyList<Notification> GetByEmployeeId(int employeeId) =>
        _notifications
            .Where(n => n.EmployeeId == employeeId)
            .OrderByDescending(n => n.ReceivedAt)
            .ToList();

    public IReadOnlyList<Notification> GetByEventType(string eventType) =>
        _notifications
            .Where(n => n.EventType == eventType)
            .OrderByDescending(n => n.ReceivedAt)
            .ToList();
}