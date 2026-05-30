using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Shared.Resilience;

public class ResilientHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly CircuitBreaker _circuitBreaker;
    private readonly ILogger _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ResilientHttpClient(
        HttpClient httpClient,
        CircuitBreaker circuitBreaker,
        ILogger logger)
    {
        _httpClient = httpClient;
        _circuitBreaker = circuitBreaker;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string path, string? token = null)
    {
        if (!_circuitBreaker.IsAvailable())
            return default;

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, path);

            if (!string.IsNullOrEmpty(token))
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                _circuitBreaker.RecordFailure();
                _logger.LogWarning(
                    "Request to {Path} failed with {StatusCode}",
                    path, response.StatusCode);
                return default;
            }

            _circuitBreaker.RecordSuccess();
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(content, JsonOptions);
        }
        catch (Exception ex)
        {
            _circuitBreaker.RecordFailure();
            _logger.LogError(ex, "Request to {Path} failed", path);
            return default;
        }
    }
}