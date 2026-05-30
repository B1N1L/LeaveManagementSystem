using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LeaveService.DTOs;
using LeaveService.Services;
using System.Security.Claims;

namespace LeaveService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LeaveController : ControllerBase
{
    private readonly LeaveManagementService _leaveService;

    public LeaveController(LeaveManagementService leaveService)
    {
        _leaveService = leaveService;
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private (int userId, string role) GetRequesterInfo()
    {
        var userIdClaim = User.FindFirst("userId")
            ?? User.FindFirst("sub");

        var roleClaim = User.FindFirst(ClaimTypes.Role)
            ?? User.FindFirst("role");

        if (userIdClaim == null || roleClaim == null)
            throw new UnauthorizedAccessException("Invalid token claims.");

        return (int.Parse(userIdClaim.Value), roleClaim.Value);
    }

    private string GetToken()
    {
        // Extract raw JWT token from Authorization header to forward to User Service
        var authHeader = Request.Headers["Authorization"].ToString();
        return authHeader.Replace("Bearer ", "");
    }

    // ── Employee Endpoints ────────────────────────────────────────────────────

    // GET /api/leave/balance
    [HttpGet("balance")]
    public async Task<IActionResult> GetMyBalance()
    {
        var (userId, role) = GetRequesterInfo();

        var balance = await _leaveService.GetLeaveBalanceAsync(userId);
        if (balance == null)
            return NotFound(new { message = "Leave balance not found." });

        return Ok(balance);
    }

    // POST /api/leave/apply
    [HttpPost("apply")]
    public async Task<IActionResult> ApplyLeave([FromBody] ApplyLeaveDto dto)
    {
        var (userId, role) = GetRequesterInfo();

        if (role != "Employee")
            return Forbid();

        var (success, error, result) = await _leaveService.ApplyLeaveAsync(userId, dto, GetToken());

        if (!success)
            return BadRequest(new { message = error });

        return CreatedAtAction(nameof(GetMyHistory), new { }, result);
    }

    // GET /api/leave/history?status=Pending&page=1&pageSize=10
    [HttpGet("history")]
    public async Task<IActionResult> GetMyHistory(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var (userId, role) = GetRequesterInfo();

        if (role != "Employee")
            return Forbid();

        // Validate pagination inputs
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 50) pageSize = 10;

        var result = await _leaveService.GetLeaveHistoryAsync(userId, status, page, pageSize);
        return Ok(result);
    }

    // PATCH /api/leave/{id}/cancel
    [HttpPatch("{id}/cancel")]
    public async Task<IActionResult> CancelLeave(int id)
    {
        var (userId, role) = GetRequesterInfo();

        if (role != "Employee")
            return Forbid();

        var (success, error) = await _leaveService.CancelLeaveAsync(id, userId);

        if (!success)
            return BadRequest(new { message = error });

        return Ok(new { message = "Leave request cancelled successfully." });
    }

    // ── Manager Endpoints ─────────────────────────────────────────────────────

    // GET /api/leave/team?status=Pending&employeeId=2&fromDate=2026-01-01&toDate=2026-12-31
    [HttpGet("team")]
    public async Task<IActionResult> GetTeamLeaveRequests([FromQuery] LeaveFilterDto filter)
    {
        var (userId, role) = GetRequesterInfo();

        if (role != "Manager")
            return Forbid();

        var result = await _leaveService.GetTeamLeaveRequestsAsync(userId, filter);
        return Ok(result);
    }

    // PATCH /api/leave/{id}/action
    [HttpPatch("{id}/action")]
    public async Task<IActionResult> ActOnLeave(int id, [FromBody] ActOnLeaveDto dto)
    {
        var (userId, role) = GetRequesterInfo();

        if (role != "Manager")
            return Forbid();

        var (success, error) = await _leaveService.ActOnLeaveAsync(id, userId, dto, GetToken());

        if (!success)
            return BadRequest(new { message = error });

        return Ok(new { message = $"Leave request {dto.Action}d successfully." });
    }

    // GET /api/leave/team/balance/{employeeId}
    // Manager can view any team member's balance
    [HttpGet("team/balance/{employeeId}")]
    public async Task<IActionResult> GetEmployeeBalance(int employeeId)
    {
        var (userId, role) = GetRequesterInfo();

        if (role != "Manager")
            return Forbid();

        var balance = await _leaveService.GetLeaveBalanceAsync(employeeId);
        if (balance == null)
            return NotFound(new { message = "Leave balance not found for this employee." });

        return Ok(balance);
    }
}