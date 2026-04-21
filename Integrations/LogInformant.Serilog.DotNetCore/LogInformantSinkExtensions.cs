using Serilog;
using Serilog.Configuration;
using Serilog.Sinks.PeriodicBatching;

namespace LogInformant.Serilog.DotNetCore;

public static class LogInformantSinkExtensions
{
    /// <summary>
    /// Adds a sink that writes log events to LogInformant API
    /// </summary>
    /// <param name="loggerConfiguration">The logger configuration</param>
    /// <param name="apiUrl">The LogInformant API URL (e.g., https://logs.yourcompany.com)</param>
    /// <param name="apiKey">The API key for authentication</param>
    /// <param name="batchSize">The maximum number of events to include in a single batch (default: 50)</param>
    /// <param name="period">The time to wait between checking for event batches (default: 2 seconds)</param>
    /// <param name="formatProvider">Supplies culture-specific formatting information, or null</param>
    /// <returns>Logger configuration allowing method chaining</returns>
    public static LoggerConfiguration LogInformant(
        this LoggerSinkConfiguration loggerConfiguration,
        string apiUrl,
        string apiKey,
        int batchSize = 50,
        TimeSpan? period = null,
        IFormatProvider? formatProvider = null)
    {
        if (loggerConfiguration == null)
            throw new ArgumentNullException(nameof(loggerConfiguration));

        if (string.IsNullOrWhiteSpace(apiUrl))
            throw new ArgumentException("API URL cannot be null or empty", nameof(apiUrl));

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("API Key cannot be null or empty", nameof(apiKey));

        var batchingOptions = new PeriodicBatchingSinkOptions
        {
            BatchSizeLimit = batchSize,
            Period = period ?? TimeSpan.FromSeconds(2),
            EagerlyEmitFirstEvent = true,
            QueueLimit = 10000
        };

        var sink = new LogInformantSink(apiUrl, apiKey, formatProvider);
        var batchingSink = new PeriodicBatchingSink(sink, batchingOptions);

        return loggerConfiguration.Sink(batchingSink);
    }
}
