using Microsoft.Extensions.Logging;

namespace Azure101.Core.Resilience;

/// <summary>
/// Provides retry policies for handling transient failures in Azure operations
/// </summary>
public class RetryPolicies
{
    private readonly ILogger<RetryPolicies> _logger;

    public RetryPolicies(ILogger<RetryPolicies> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Executes an operation with exponential backoff retry logic
    /// Default: 3 retries with 1s, 2s, 4s delays
    /// </summary>
    public async Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> operation,
        int maxRetries = 3,
        int initialDelayMs = 1000,
        string operationName = "Operation")
    {
        int delay = initialDelayMs;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                _logger.LogInformation("{OperationName} - Attempt {Attempt}/{MaxRetries}",
                    operationName, attempt, maxRetries);
                return await operation();
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                _logger.LogWarning(ex,
                    "{OperationName} - Attempt {Attempt}/{MaxRetries} failed. Retrying after {DelayMs}ms",
                    operationName, attempt, maxRetries, delay);
                await Task.Delay(delay);
                delay *= 2; // Exponential backoff
            }
        }

        // Final attempt without catch
        _logger.LogInformation("{OperationName} - Final attempt {MaxRetries}/{MaxRetries}",
            operationName, maxRetries);
        return await operation();
    }

    /// <summary>
    /// Executes an operation with exponential backoff retry logic (void operation)
    /// </summary>
    public async Task ExecuteWithRetryAsync(
        Func<Task> operation,
        int maxRetries = 3,
        int initialDelayMs = 1000,
        string operationName = "Operation")
    {
        int delay = initialDelayMs;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                _logger.LogInformation("{OperationName} - Attempt {Attempt}/{MaxRetries}",
                    operationName, attempt, maxRetries);
                await operation();
                return;
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                _logger.LogWarning(ex,
                    "{OperationName} - Attempt {Attempt}/{MaxRetries} failed. Retrying after {DelayMs}ms",
                    operationName, attempt, maxRetries, delay);
                await Task.Delay(delay);
                delay *= 2;
            }
        }

        // Final attempt without catch
        _logger.LogInformation("{OperationName} - Final attempt {MaxRetries}/{MaxRetries}",
            operationName, maxRetries);
        await operation();
    }
}
