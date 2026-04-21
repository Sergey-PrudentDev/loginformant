using Serilog;
using Serilog.Configuration;
using Serilog.Sinks.PeriodicBatching;
using System;

namespace LogInformant.Serilog.DotNetFramework
{
    public static class LogInformantSinkExtensions
    {
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
                throw new ArgumentException("API URL cannot be empty", nameof(apiUrl));
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentException("API Key cannot be empty", nameof(apiKey));

            var options = new PeriodicBatchingSinkOptions
            {
                BatchSizeLimit = batchSize,
                Period = period ?? TimeSpan.FromSeconds(2),
                EagerlyEmitFirstEvent = true,
                QueueLimit = 10000
            };

            var sink = new LogInformantSink(apiUrl, apiKey, formatProvider);
            var batchingSink = new PeriodicBatchingSink(sink, options);
            return loggerConfiguration.Sink(batchingSink);
        }
    }
}
