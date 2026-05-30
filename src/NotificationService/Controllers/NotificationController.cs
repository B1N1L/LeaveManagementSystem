using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationService.Services;
using System.Security.Claims;

namespace NotificationService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly NotificationStore _store;

    public NotificationController(NotificationStore store)
    {
        _store = store;
    }

    private (int userId, string role) GetRequesterInfo()
    {
        var userIdClaim = User.FindFirst("userId") ?? User.FindFirst("sub");
        var roleClaim = User.FindFirst(ClaimTypes.Role) ?? User.FindFirst("role");

        if (userIdClaim == null || roleClaim == null)
            throw new UnauthorizedAccessException("Invalid token claims.");

        return (int.Parse(userIdClaim.Value), roleClaim.Value);
    }

    // GET /api/notification
    // Managers see all notifications, employees see only their own
    [HttpGet]
    public IActionResult GetNotifications([FromQuery] string? eventType)
    {
        var (userId, role) = GetRequesterInfo();

        var notifications = role == "Manager"
            ? _store.GetAll()
            : _store.GetByEmployeeId(userId);

        // Optional filter by event type
        if (!string.IsNullOrEmpty(eventType))
            notifications = notifications
                .Where(n => n.EventType == eventType)
                .ToList();

        return Ok(new
        {
            count = notifications.Count,
            notifications
        });
    }

    // GET /api/notification/employee/{employeeId}
    // Manager only — view specific employee's notifications
    [HttpGet("employee/{employeeId}")]
    public IActionResult GetByEmployee(int employeeId)
    {
        var (userId, role) = GetRequesterInfo();

        if (role != "Manager")
            return Forbid();

        var notifications = _store.GetByEmployeeId(employeeId);
        return Ok(new
        {
            count = notifications.Count,
            notifications
        });
    }
}