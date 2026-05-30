using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.DTOs;
using UserService.Services;
using UserService.Messaging;
using System.Security.Claims;
namespace UserService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly UserCreatedPublisher _userCreatedPublisher;

    public AuthController(AuthService authService, UserCreatedPublisher userCreatedPublisher)
    {
        _authService = authService;
        _userCreatedPublisher = userCreatedPublisher;
    }

    // POST /api/auth/login
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            return BadRequest(new { message = "Email and password are required." });

        var result = await _authService.LoginAsync(request);

        if (result == null)
            return Unauthorized(new { message = "Invalid email or password." });

        return Ok(result);
    }

    // GET /api/auth/users/{id}
    // Called internally by other services to get user info
    [HttpGet("users/{id}")]
    [Authorize]
    public async Task<IActionResult> GetUserById(int id)
    {
        // Extract claims from the JWT token
        var (requesterId, requesterRole) = GetRequesterInfo();

        // Employees can only view their own data
        if (requesterRole == "Employee" && requesterId != id)
            return Forbid(); // 403 Forbidden

        var user = await _authService.GetUserByIdAsync(id);
        if (user == null)
            return NotFound(new { message = "User not found." });

        // Managers can only view their own team members
        if (requesterRole == "Manager")
        {
            // Manager can always view their own profile
            if (requesterId == id)
                return Ok(user);

            // Check if requested user belongs to this manager
            if (user.ManagerId != requesterId)
                return Forbid(); // 403 Forbidden
        }

        return Ok(user);
    }

    // GET /api/auth/users/{managerId}/employees
    // Called by Leave Service to get all employees under a manager
    [HttpGet("users/{managerId}/employees")]
    [Authorize]
    public async Task<IActionResult> GetEmployeesByManager(int managerId)
    {
        var (requesterId, requesterRole) = GetRequesterInfo();

        // Only managers can access this endpoint
        if (requesterRole != "Manager")
            return Forbid(); // 403 Forbidden

        // Manager can only see their own team, not another manager's team
        if (requesterId != managerId)
            return Forbid(); // 403 Forbidden

        var employees = await _authService.GetEmployeesByManagerIdAsync(managerId);
        return Ok(employees);
    }

    // POST /api/auth/users
    [HttpPost("users")]
    [Authorize]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequestDto request)
    {
        var (requesterId, requesterRole) = GetRequesterInfo();

        // Only managers can create users
        if (requesterRole != "Manager")
            return Forbid();

        // Basic input validation
        if (string.IsNullOrWhiteSpace(request.FullName))
            return BadRequest(new { message = "Full name is required." });

        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { message = "Email is required." });

        if (string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Password is required." });

        if (string.IsNullOrWhiteSpace(request.Role))
            return BadRequest(new { message = "Role is required." });

        var (success, error, result) = await _authService.CreateUserAsync(request, requesterId);

        if (!success)
            return BadRequest(new { message = error });

        // Publish UserCreated event to RabbitMQ
        await _userCreatedPublisher.PublishAsync(new UserCreatedMessage
        {
            UserId = result!.Id,
            FullName = result.FullName,
            Email = result.Email,
            Role = result.Role
        });

        return CreatedAtAction(nameof(GetUserById), new { id = result.Id }, result);
    }

    private (int userId, string role) GetRequesterInfo()
    {
        // Try all possible ways .NET might store the claim
        var userIdClaim = User.FindFirst("userId")
            ?? User.FindFirst("sub");

        var roleClaim = User.FindFirst("role")
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.Role);

        if (userIdClaim == null || roleClaim == null)
            throw new UnauthorizedAccessException("Invalid token claims.");

        return (int.Parse(userIdClaim.Value), roleClaim.Value);
    }
}