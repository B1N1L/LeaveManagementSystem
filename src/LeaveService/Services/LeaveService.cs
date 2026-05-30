using Microsoft.EntityFrameworkCore;
using LeaveService.Data;
using LeaveService.DTOs;
using LeaveService.Messaging;
using LeaveService.Models;

namespace LeaveService.Services;

public class LeaveManagementService
{
    private readonly LeaveDbContext _context;
    private readonly RabbitMQPublisher _publisher;
    private readonly UserServiceClient _userServiceClient;
    private readonly ILogger<LeaveManagementService> _logger;

    // Valid leave types
    private static readonly string[] ValidLeaveTypes = { "Sick", "Casual", "Privilege" };

    public LeaveManagementService(
        LeaveDbContext context,
        RabbitMQPublisher publisher,
        UserServiceClient userServiceClient,
        ILogger<LeaveManagementService> logger)
    {
        _context = context;
        _publisher = publisher;
        _userServiceClient = userServiceClient;
        _logger = logger;
    }

    // ── Leave Balance ─────────────────────────────────────────────────────────

    public async Task<LeaveBalanceDto?> GetLeaveBalanceAsync(int employeeId)
    {
        var balance = await _context.LeaveBalances
            .FirstOrDefaultAsync(lb => lb.EmployeeId == employeeId
                                    && lb.Year == DateTime.UtcNow.Year);
        if (balance == null)
            return null;

        return MapToBalanceDto(balance);
    }

    // Called when a new employee is created — initializes their leave balance
    public async Task InitializeLeaveBalanceAsync(int employeeId)
    {
        var currentYear = DateTime.UtcNow.Year;

        // Don't create duplicate balance
        var exists = await _context.LeaveBalances
            .AnyAsync(lb => lb.EmployeeId == employeeId && lb.Year == currentYear);

        if (exists) return;

        var balance = new LeaveBalance
        {
            EmployeeId = employeeId,
            Year = currentYear,
            TotalSickLeaves = 10,
            TotalCasualLeaves = 12,
            TotalPrivilegeLeaves = 15
        };

        _context.LeaveBalances.Add(balance);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Initialized leave balance for employee {EmployeeId}", employeeId);
    }

    // ── Apply Leave ───────────────────────────────────────────────────────────

    public async Task<(bool Success, string Error, LeaveRequestDto? Result)>
        ApplyLeaveAsync(int employeeId, ApplyLeaveDto dto, string token)
    {
        // Validate leave type
        if (!ValidLeaveTypes.Contains(dto.LeaveType))
            return (false, $"Invalid leave type. Valid types: {string.Join(", ", ValidLeaveTypes)}", null);

        // Validate date range — no past dates
        if (dto.StartDate.Date < DateTime.UtcNow.Date)
            return (false, "Start date cannot be in the past.", null);

        // Validate start <= end
        if (dto.StartDate.Date > dto.EndDate.Date)
            return (false, "Start date cannot be after end date.", null);

        // Calculate total days (excluding weekends)
        int totalDays = CalculateWorkingDays(dto.StartDate, dto.EndDate);
        if (totalDays == 0)
            return (false, "Leave request must include at least one working day.", null);

        // Fetch employee info from User Service to get their ManagerId
        var employee = await _userServiceClient.GetUserByIdAsync(employeeId, token);
        if (employee == null)
            return (false, "Employee not found.", null);

        if (employee.ManagerId == null)
            return (false, "No manager assigned to this employee.", null);

        // Check for overlapping leave requests
        var startDate = ToUtc(dto.StartDate);
        var endDate = ToUtc(dto.EndDate);
        var hasOverlap = await _context.LeaveRequests.AnyAsync(lr =>
            lr.EmployeeId == employeeId &&
            lr.Status != "Rejected" &&
            lr.Status != "Cancelled" &&
            lr.StartDate <= endDate &&
            lr.EndDate >= startDate);

        if (hasOverlap)
            return (false, "You already have a leave request overlapping these dates.", null);

        // Check sufficient balance
        var balance = await _context.LeaveBalances
            .FirstOrDefaultAsync(lb => lb.EmployeeId == employeeId
                                    && lb.Year == startDate.Year);

        if (balance == null)
            return (false, "Leave balance not found. Please contact HR.", null);

        var remaining = GetRemainingLeaves(balance, dto.LeaveType);
        if (remaining < totalDays)
            return (false, $"Insufficient {dto.LeaveType} leave balance. Available: {remaining} days, Requested: {totalDays} days.", null);

        // All validations passed — create the leave request
        var leaveRequest = new LeaveRequest
        {
            EmployeeId = employeeId,
            ManagerId = employee.ManagerId.Value,
            LeaveType = dto.LeaveType,
            StartDate = startDate,          
            EndDate = endDate,
            TotalDays = totalDays,
            Reason = dto.Reason,
            Status = "Pending",
            AppliedOn = DateTime.UtcNow
        };

        _context.LeaveRequests.Add(leaveRequest);
        await _context.SaveChangesAsync();

        // Log notification for both employee and manager
        _logger.LogInformation(
            "[NOTIFICATION] LEAVE APPLICATION SUBMITTED — " +
            "Employee: {EmployeeName} (ID: {EmployeeId}) | " +
            "Manager ID: {ManagerId} | " +
            "Type: {LeaveType} | " +
            "From: {StartDate:yyyy-MM-dd} To: {EndDate:yyyy-MM-dd} | " +
            "Days: {TotalDays} | " +
            "Reason: {Reason} | " +
            "Status: Pending | " +
            "Applied On: {AppliedOn:yyyy-MM-dd HH:mm:ss}",
            employee.FullName, employeeId,
            employee.ManagerId,
            dto.LeaveType,
            startDate, endDate,
            totalDays,
            dto.Reason,
            DateTime.UtcNow);

        return (true, string.Empty, MapToRequestDto(leaveRequest));
    }

    // ── Manager Actions ───────────────────────────────────────────────────────

    public async Task<List<LeaveRequestDto>> GetTeamLeaveRequestsAsync(
        int managerId, LeaveFilterDto filter)
    {
        var query = _context.LeaveRequests
            .Where(lr => lr.ManagerId == managerId)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(filter.Status))
            query = query.Where(lr => lr.Status == filter.Status);

        if (filter.EmployeeId.HasValue)
            query = query.Where(lr => lr.EmployeeId == filter.EmployeeId.Value);

        if (filter.FromDate.HasValue)
            query = query.Where(lr => lr.StartDate.Date >= filter.FromDate.Value.Date);

        if (filter.ToDate.HasValue)
            query = query.Where(lr => lr.EndDate.Date <= filter.ToDate.Value.Date);

        return await query
            .OrderByDescending(lr => lr.AppliedOn)
            .Select(lr => MapToRequestDto(lr))
            .ToListAsync();
    }

    public async Task<(bool Success, string Error)> ActOnLeaveAsync(
        int leaveRequestId, int managerId, ActOnLeaveDto dto, string token)
    {
        // Validate action
        if (dto.Action != "Approve" && dto.Action != "Reject")
        {
            _logger.LogWarning(
                "[ERROR] Invalid action '{Action}' attempted by manager {ManagerId}",
                dto.Action, managerId);
            return (false, "Action must be either 'Approve' or 'Reject'.");
        }

        // Rejection requires a reason
        if (dto.Action == "Reject" && string.IsNullOrWhiteSpace(dto.RejectionReason))
        {
            _logger.LogWarning(
                "[ERROR] Rejection attempted without reason by manager {ManagerId}",
                managerId);
            return (false, "Rejection reason is required.");
        }

        var leaveRequest = await _context.LeaveRequests.FindAsync(leaveRequestId);

        if (leaveRequest == null)
        {
            _logger.LogWarning(
                "[ERROR] Leave request {LeaveRequestId} not found. Manager: {ManagerId}",
                leaveRequestId, managerId);
            return (false, "Leave request not found.");
        }

        // Manager can only act on their own team's requests
        if (leaveRequest.ManagerId != managerId)
        {
            _logger.LogWarning(
                "[ERROR] Unauthorized action — Manager {ManagerId} tried to act on " +
                "leave request {LeaveRequestId} belonging to manager {ActualManagerId}",
                managerId, leaveRequestId, leaveRequest.ManagerId);
            return (false, "You are not authorized to act on this leave request.");
        }

        // Can only act on pending requests
        if (leaveRequest.Status != "Pending")
        {
            _logger.LogWarning(
                "[ERROR] Cannot act on leave request {LeaveRequestId} " +
                "with status '{Status}'",
                leaveRequestId, leaveRequest.Status);
            return (false, $"Cannot act on a leave request with status '{leaveRequest.Status}'.");
        }

        if (dto.Action == "Approve")
        {
            // Deduct from leave balance
            var balance = await _context.LeaveBalances
                .FirstOrDefaultAsync(lb => lb.EmployeeId == leaveRequest.EmployeeId
                                        && lb.Year == leaveRequest.StartDate.Year);

            if (balance == null)
                return (false, "Employee leave balance not found.");

            // Re-check balance at time of approval
            var remaining = GetRemainingLeaves(balance, leaveRequest.LeaveType);
            if (remaining < leaveRequest.TotalDays)
                return (false, $"Employee no longer has sufficient {leaveRequest.LeaveType} balance.");

            DeductLeaveBalance(balance, leaveRequest.LeaveType, leaveRequest.TotalDays);
            leaveRequest.Status = "Approved";
        }
        else
        {
            leaveRequest.Status = "Rejected";
            leaveRequest.RejectionReason = dto.RejectionReason;
        }

        leaveRequest.ActedOn = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Fetch employee name for notification
        var employee = await _userServiceClient.GetUserByIdAsync(leaveRequest.EmployeeId, token);

        // Publish notification to RabbitMQ
        await _publisher.PublishAsync(new LeaveNotificationMessage
        {
            EventType = dto.Action == "Approve" ? "LeaveApproved" : "LeaveRejected",
            EmployeeId = leaveRequest.EmployeeId,
            EmployeeName = employee?.FullName ?? "Unknown",
            LeaveType = leaveRequest.LeaveType,
            StartDate = leaveRequest.StartDate,
            EndDate = leaveRequest.EndDate,
            TotalDays = leaveRequest.TotalDays,
            RejectionReason = leaveRequest.RejectionReason,
            ActedOn = leaveRequest.ActedOn.Value
        });

        _logger.LogInformation(
            "Leave request {LeaveRequestId} {Action}d by manager {ManagerId}",
            leaveRequestId, dto.Action, managerId);

        return (true, string.Empty);
    }

    // ── Employee Actions ──────────────────────────────────────────────────────

    public async Task<PagedResultDto<LeaveRequestDto>> GetLeaveHistoryAsync(
        int employeeId, string? status, int page, int pageSize)
    {
        var query = _context.LeaveRequests
            .Where(lr => lr.EmployeeId == employeeId)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(lr => lr.Status == status);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(lr => lr.AppliedOn)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(lr => MapToRequestDto(lr))
            .ToListAsync();

        return new PagedResultDto<LeaveRequestDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<(bool Success, string Error)> CancelLeaveAsync(
        int leaveRequestId, int employeeId)
    {
        var leaveRequest = await _context.LeaveRequests.FindAsync(leaveRequestId);

        if (leaveRequest == null)
            return (false, "Leave request not found.");

        // Employee can only cancel their own leave
        if (leaveRequest.EmployeeId != employeeId)
            return (false, "You are not authorized to cancel this leave request.");

        // Only pending leaves can be cancelled
        if (leaveRequest.Status != "Pending")
            return (false, $"Only pending leave requests can be cancelled. Current status: {leaveRequest.Status}");

        leaveRequest.Status = "Cancelled";
        leaveRequest.ActedOn = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "[NOTIFICATION] LEAVE CANCELLED — " +
            "Employee ID: {EmployeeId} | " +
            "Leave Request ID: {LeaveRequestId} | " +
            "Type: {LeaveType} | " +
            "From: {StartDate:yyyy-MM-dd} To: {EndDate:yyyy-MM-dd} | " +
            "Cancelled On: {CancelledOn:yyyy-MM-dd HH:mm:ss}",
            employeeId,
            leaveRequestId,
            leaveRequest.LeaveType,
            leaveRequest.StartDate,
            leaveRequest.EndDate,
            DateTime.UtcNow);

        return (true, string.Empty);
    }

    // ── Private Helpers ───────────────────────────────────────────────────────

    private static int CalculateWorkingDays(DateTime start, DateTime end)
    {
        int workingDays = 0;
        for (var date = start.Date; date <= end.Date; date = date.AddDays(1))
        {
            if (date.DayOfWeek != DayOfWeek.Saturday &&
                date.DayOfWeek != DayOfWeek.Sunday)
                workingDays++;
        }
        return workingDays;
    }

    private static int GetRemainingLeaves(LeaveBalance balance, string leaveType)
    {
        return leaveType switch
        {
            "Sick" => balance.TotalSickLeaves - balance.UsedSickLeaves,
            "Casual" => balance.TotalCasualLeaves - balance.UsedCasualLeaves,
            "Privilege" => balance.TotalPrivilegeLeaves - balance.UsedPrivilegeLeaves,
            _ => 0
        };
    }

    private static void DeductLeaveBalance(LeaveBalance balance, string leaveType, int days)
    {
        switch (leaveType)
        {
            case "Sick": balance.UsedSickLeaves += days; break;
            case "Casual": balance.UsedCasualLeaves += days; break;
            case "Privilege": balance.UsedPrivilegeLeaves += days; break;
        }
    }

    private static LeaveRequestDto MapToRequestDto(LeaveRequest lr) => new()
    {
        Id = lr.Id,
        EmployeeId = lr.EmployeeId,
        ManagerId = lr.ManagerId,
        LeaveType = lr.LeaveType,
        StartDate = lr.StartDate,
        EndDate = lr.EndDate,
        TotalDays = lr.TotalDays,
        Status = lr.Status,
        Reason = lr.Reason,
        RejectionReason = lr.RejectionReason,
        AppliedOn = lr.AppliedOn,
        ActedOn = lr.ActedOn
    };

    private static LeaveBalanceDto MapToBalanceDto(LeaveBalance lb) => new()
    {
        EmployeeId = lb.EmployeeId,
        Year = lb.Year,
        TotalSickLeaves = lb.TotalSickLeaves,
        UsedSickLeaves = lb.UsedSickLeaves,
        RemainingSickLeaves = lb.TotalSickLeaves - lb.UsedSickLeaves,
        TotalCasualLeaves = lb.TotalCasualLeaves,
        UsedCasualLeaves = lb.UsedCasualLeaves,
        RemainingCasualLeaves = lb.TotalCasualLeaves - lb.UsedCasualLeaves,
        TotalPrivilegeLeaves = lb.TotalPrivilegeLeaves,
        UsedPrivilegeLeaves = lb.UsedPrivilegeLeaves,
        RemainingPrivilegeLeaves = lb.TotalPrivilegeLeaves - lb.UsedPrivilegeLeaves
    };

    private static DateTime ToUtc(DateTime date) =>
    DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
}