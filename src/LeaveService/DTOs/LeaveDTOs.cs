namespace LeaveService.DTOs;

// What employee sends when applying for leave
public class ApplyLeaveDto
{
    public string LeaveType { get; set; } = string.Empty;   // Sick, Casual, Privilege
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Reason { get; set; } = string.Empty;
}

// What manager sends when approving or rejecting
public class ActOnLeaveDto
{
    public string Action { get; set; } = string.Empty;      // "Approve" or "Reject"
    public string? RejectionReason { get; set; }            // Required only if rejecting
}

// What we return when showing a leave request
public class LeaveRequestDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public int ManagerId { get; set; }
    public string LeaveType { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalDays { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? RejectionReason { get; set; }
    public DateTime AppliedOn { get; set; }
    public DateTime? ActedOn { get; set; }
}

// What we return when showing leave balance
public class LeaveBalanceDto
{
    public int EmployeeId { get; set; }
    public int Year { get; set; }
    public int TotalSickLeaves { get; set; }
    public int UsedSickLeaves { get; set; }
    public int RemainingSickLeaves { get; set; }
    public int TotalCasualLeaves { get; set; }
    public int UsedCasualLeaves { get; set; }
    public int RemainingCasualLeaves { get; set; }
    public int TotalPrivilegeLeaves { get; set; }
    public int UsedPrivilegeLeaves { get; set; }
    public int RemainingPrivilegeLeaves { get; set; }
}

// Filters for manager viewing team leave requests
public class LeaveFilterDto
{
    public string? Status { get; set; }                     // Pending, Approved, Rejected, Cancelled
    public int? EmployeeId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

// Pagination wrapper — reusable for any list response
public class PagedResultDto<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

// Message published to RabbitMQ
public class LeaveNotificationMessage
{
    public string EventType { get; set; } = string.Empty;  // "LeaveApproved" or "LeaveRejected"
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string LeaveType { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalDays { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime ActedOn { get; set; }
}