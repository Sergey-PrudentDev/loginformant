using NLog;
using NLog.Common;
using NLog.Config;
using NLog.Targets;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace LogInformant.NLog
{
    [Target("LogInformant")]
    public sealed class LogInformantTarget : AsyncTaskTarget
    {
        private static readonly HttpClient SharedHttpClient = new HttpClient();

        [RequiredParameter]
        public string ApiUrl { get; set; } = string.Empty;

        [RequiredParameter]
        public string ApiKey { get; set; } = string.Empty;

        protected override async Task WriteAsyncTask(LogEventInfo logEvent, CancellationToken token)
        {
            await SendBatchAsync(new[] { logEvent }, token).ConfigureAwait(false);
        }

        protected override async Task WriteAsyncTask(IList<LogEventInfo> logEvents, CancellationToken token)
        {
            await SendBatchAsync(logEvents, token).ConfigureAwait(false);
        }

        private async Task SendBatchAsync(IList<LogEventInfo> logEvents, CancellationToken token)
        {
            var logs = new List<object>(logEvents.Count);
            foreach (var e in logEvents)
            {
                logs.Add(new
                {
                    timestamp = e.TimeStamp.ToUniversalTime(),
                    level = MapLevel(e.Level),
                    message = e.FormattedMessage,
                    exception = e.Exception?.ToString(),
                    source = e.LoggerName,
                    properties = GetProperties(e)
                });
            }

            var json = JsonSerializer.Serialize(new { logs });

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl.TrimEnd('/') + "/api/ingest/batch");
                request.Headers.Add("X-API-Key", ApiKey);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await SharedHttpClient.SendAsync(request, token).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                InternalLogger.Warn(ex, "LogInformant: Failed to send batch");
            }
        }

        private static string MapLevel(LogLevel level)
        {
            if (level == LogLevel.Trace || level == LogLevel.Debug) return "Debug";
            if (level == LogLevel.Info) return "Information";
            if (level == LogLevel.Warn) return "Warning";
            if (level == LogLevel.Error) return "Error";
            if (level == LogLevel.Fatal) return "Fatal";
            return "Information";
        }

        private static Dictionary<string, object>? GetProperties(LogEventInfo e)
        {
            if (e.Parameters == null || e.Parameters.Length == 0) return null;
            var dict = new Dictionary<string, object>();
            for (int i = 0; i < e.Parameters.Length; i++)
            {
                if (e.Parameters[i] != null)
                    dict[$"arg{i}"] = e.Parameters[i]!;
            }
            return dict.Count > 0 ? dict : null;
        }
    }
}
