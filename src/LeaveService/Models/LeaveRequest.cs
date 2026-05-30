namespace LeaveService.Models;

public class LeaveRequest
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public int ManagerId { get; set; }                      // Auto-fetched from User Service
    public string LeaveType { get; set; } = string.Empty;  // Sick, Casual, Privilege
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalDays { get; set; }
    public string Status { get; set; } = "Pending";        // Pending, Approved, Rejected, Cancelled
    public string Reason { get; set; } = string.Empty;
    public string? RejectionReason { get; set; }
    public DateTime AppliedOn { get; set; } = DateTime.UtcNow;
    public DateTime? ActedOn { get; set; }
}