using Microsoft.Extensions.Logging;

namespace Shared.Resilience;

public class CircuitBreaker
{
    private int _failureCount = 0;
    private DateTime _lastFailureTime = DateTime.MinValue;
    private bool _isOpen = false;

    private readonly int _failureThreshold;
    private readonly TimeSpan _breakDuration;
    private readonly ILogger _logger;
    private readonly string _serviceName;

    public CircuitBreaker(
        string serviceName,
        ILogger logger,
        int failureThreshold = 3,
        int breakDurationSeconds = 30)
    {
        _serviceName = serviceName;
        _logger = logger;
        _failureThreshold = failureThreshold;
        _breakDuration = TimeSpan.FromSeconds(breakDurationSeconds);
    }

    public bool IsAvailable()
    {
        if (!_isOpen)
            return true;

        // Check if break duration has passed
        if (DateTime.UtcNow - _lastFailureTime > _breakDuration)
        {
            _logger.LogInformation(
                "Circuit breaker for {ServiceName} is HALF-OPEN — attempting request",
                _serviceName);
            _isOpen = false;
            _failureCount = 0;
            return true;
        }

        var remainingSeconds = (_breakDuration - (DateTime.UtcNow - _lastFailureTime)).TotalSeconds;
        _logger.LogWarning(
            "Circuit breaker for {ServiceName} is OPEN — " +
            "requests blocked for {Seconds:F0} more seconds",
            _serviceName, remainingSeconds);

        return false;
    }

    public void RecordSuccess()
    {
        if (_failureCount > 0 || _isOpen)
        {
            _logger.LogInformation(
                "Circuit breaker for {ServiceName} CLOSED — service recovered",
                _serviceName);
        }
        _failureCount = 0;
        _isOpen = false;
    }

    public void RecordFailure()
    {
        _failureCount++;
        _lastFailureTime = DateTime.UtcNow;

        if (_failureCount >= _failureThreshold)
        {
            _isOpen = true;
            _logger.LogWarning(
                "Circuit breaker for {ServiceName} OPENED after {Count} failures. " +
                "Blocking requests for {Duration} seconds",
                _serviceName, _failureCount, _breakDuration.TotalSeconds);
        }
        else
        {
            _logger.LogWarning(
                "Circuit breaker for {ServiceName} recorded failure " +
                "{Count}/{Threshold}",
                _serviceName, _failureCount, _failureThreshold);
        }
    }
}