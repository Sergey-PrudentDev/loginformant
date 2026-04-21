using Serilog.Debugging;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using PeriodicBatching = Serilog.Sinks.PeriodicBatching;

namespace LogInformant.Serilog.DotNetFramework
{
    public class LogInformantSink : PeriodicBatching.IBatchedLogEventSink
    {
        private static readonly HttpClient SharedHttpClient = new HttpClient();

        private readonly string _apiUrl;
        private readonly string _apiKey;
        private readonly IFormatProvider? _formatProvider;

        public LogInformantSink(string apiUrl, string apiKey, IFormatProvider? formatProvider = null)
        {
            _apiUrl = apiUrl.TrimEnd('/') + "/api/ingest/batch";
            _apiKey = apiKey;
            _formatProvider = formatProvider;
        }

        public async Task EmitBatchAsync(IEnumerable<LogEvent> batch)
        {
            var logs = batch.Select(logEvent => new
            {
                timestamp = logEvent.Timestamp.ToUniversalTime().DateTime,
                level = GetLogLevel(logEvent.Level),
                message = logEvent.RenderMessage(_formatProvider),
                exception = logEvent.Exception?.ToString(),
                source = GetSourceContext(logEvent),
                properties = GetProperties(logEvent)
            }).ToList();

            var payload = new { logs };
            var json = JsonSerializer.Serialize(payload);

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, _apiUrl);
                request.Headers.Add("X-API-Key", _apiKey);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await SharedHttpClient.SendAsync(request).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                SelfLog.WriteLine("[LogInformant] Failed to send batch: {0}", ex.Message);
            }
        }

        public Task OnEmptyBatchAsync() => Task.CompletedTask;

        private static string GetLogLevel(LogEventLevel level)
        {
            switch (level)
            {
                case LogEventLevel.Verbose: return "Debug";
                case LogEventLevel.Debug: return "Debug";
                case LogEventLevel.Information: return "Information";
                case LogEventLevel.Warning: return "Warning";
                case LogEventLevel.Error: return "Error";
                case LogEventLevel.Fatal: return "Fatal";
                default: return "Information";
            }
        }

        private static string? GetSourceContext(LogEvent logEvent)
        {
            if (logEvent.Properties.TryGetValue("SourceContext", out var sc))
                return sc.ToString().Trim('"');
            return null;
        }

        private static Dictionary<string, object>? GetProperties(LogEvent logEvent)
        {
            var props = new Dictionary<string, object>();
            foreach (var prop in logEvent.Properties)
            {
                if (prop.Key == "SourceContext") continue;
                props[prop.Key] = prop.Value.ToString();
            }
            return props.Count > 0 ? props : null;
        }
    }
}
