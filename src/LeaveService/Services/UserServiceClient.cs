using Shared.Resilience;
using Microsoft.Extensions.Logging;

namespace LeaveService.Services;

public class UserInfoDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int? ManagerId { get; set; }
}

public class UserServiceClient
{
    private readonly ResilientHttpClient _resilientClient;

    // Static — shared across ALL instances of UserServiceClient
    // This ensures failure count persists between requests
    private static CircuitBreaker? _circuitBreaker;
    private static readonly object _lock = new();

    public UserServiceClient(
        HttpClient httpClient,
        ILogger<UserServiceClient> logger)
    {
        // Thread-safe singleton initialization of circuit breaker
        if (_circuitBreaker == null)
        {
            lock (_lock)
            {
                _circuitBreaker ??= new CircuitBreaker(
                    serviceName: "UserService",
                    logger: logger,
                    failureThreshold: 3,
                    breakDurationSeconds: 30);
            }
        }

        _resilientClient = new ResilientHttpClient(httpClient, _circuitBreaker, logger);
    }

    public async Task<UserInfoDto?> GetUserByIdAsync(int userId, string token)
    {
        return await _resilientClient.GetAsync<UserInfoDto>(
            $"/api/auth/users/{userId}", token);
    }
}