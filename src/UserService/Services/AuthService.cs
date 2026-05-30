using Microsoft.EntityFrameworkCore;
using UserService.Data;
using UserService.DTOs;
using UserService.Helpers;
using UserService.Models;

namespace UserService.Services;

public class AuthService
{
    private readonly UserDbContext _context;
    private readonly JwtHelper _jwtHelper;
    private readonly ILogger<AuthService> _logger;

    public AuthService(UserDbContext context, JwtHelper jwtHelper, ILogger<AuthService> logger)
    {
        _context = context;
        _jwtHelper = jwtHelper;
        _logger = logger;
    }

    public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto request)
    {
        // Find user by email
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        // User not found
        if (user == null)
            return null;

        // Verify password against stored hash
        bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
        if (!isPasswordValid)
            return null;

        // Generate JWT token
        var token = _jwtHelper.GenerateToken(user);

        return new LoginResponseDto
        {
            Token = token,
            FullName = user.FullName,
            Role = user.Role,
            UserId = user.Id
        };
    }

    public async Task<UserInfoDto?> GetUserByIdAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return null;

        return new UserInfoDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            ManagerId = user.ManagerId
        };
    }

    public async Task<List<UserInfoDto>> GetEmployeesByManagerIdAsync(int managerId)
    {
        return await _context.Users
            .Where(u => u.ManagerId == managerId)
            .Select(u => new UserInfoDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                Role = u.Role,
                ManagerId = u.ManagerId
            })
            .ToListAsync();
    }

    public async Task<(bool Success, string Error, CreateUserResponseDto? Result)>
    CreateUserAsync(CreateUserRequestDto request, int createdByManagerId)
    {
        // Validate role
        if (request.Role != "Employee" && request.Role != "Manager")
            return (false, "Role must be either 'Employee' or 'Manager'.", null);

        // Employee must have a manager assigned
        if (request.Role == "Employee" && request.ManagerId == null)
            return (false, "ManagerId is required when creating an Employee.", null);

        // If ManagerId is provided, verify that manager exists
        if (request.ManagerId.HasValue)
        {
            var manager = await _context.Users.FindAsync(request.ManagerId.Value);
            if (manager == null)
                return (false, "Specified manager does not exist.", null);

            if (manager.Role != "Manager")
                return (false, "Specified ManagerId does not belong to a Manager.", null);
        }

        // Check if email already exists
        var emailExists = await _context.Users
            .AnyAsync(u => u.Email == request.Email);

        if (emailExists)
            return (false, "A user with this email already exists.", null);

        // Validate password length
        if (request.Password.Length < 6)
            return (false, "Password must be at least 6 characters.", null);

        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = request.Role,
            ManagerId = request.ManagerId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "New user created: {Email}, Role: {Role}, by Manager: {ManagerId}",
            user.Email, user.Role, createdByManagerId);

        return (true, string.Empty, new CreateUserResponseDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            ManagerId = user.ManagerId,
            CreatedAt = user.CreatedAt
        });
    }
}