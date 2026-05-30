namespace NotificationService.Models;

public class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EventType { get; set; } = string.Empty;     // LeaveApproved / LeaveRejected
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string LeaveType { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalDays { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime ActedOn { get; set; }
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
}