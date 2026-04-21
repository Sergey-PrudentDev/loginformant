using log4net.Appender;
using log4net.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace LogInformant.Log4Net
{
    public sealed class LogInformantAppender : AppenderSkeleton
    {
        private static readonly HttpClient SharedHttpClient = new HttpClient();

        private readonly BlockingCollection<LoggingEvent> _queue = new BlockingCollection<LoggingEvent>(10000);
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private Thread? _flushThread;

        public string ApiUrl { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public int BatchSize { get; set; } = 50;
        public int FlushIntervalMs { get; set; } = 2000;

        public override void ActivateOptions()
        {
            base.ActivateOptions();
            _flushThread = new Thread(FlushLoop) { IsBackground = true, Name = "LogInformant-Flush" };
            _flushThread.Start();
        }

        protected override void Append(LoggingEvent loggingEvent)
        {
            loggingEvent.Fix = FixFlags.All;
            _queue.TryAdd(loggingEvent);
        }

        protected override void OnClose()
        {
            _cts.Cancel();
            _flushThread?.Join(3000);
            FlushQueue().GetAwaiter().GetResult();
            _queue.Dispose();
            base.OnClose();
        }

        private void FlushLoop()
        {
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    Thread.Sleep(FlushIntervalMs);
                    FlushQueue().GetAwaiter().GetResult();
                }
                catch (ThreadInterruptedException) { break; }
                catch { /* never crash the background thread */ }
            }
        }

        private async Task FlushQueue()
        {
            var batch = new List<LoggingEvent>(BatchSize);
            while (batch.Count < BatchSize && _queue.TryTake(out var e))
                batch.Add(e);

            if (batch.Count == 0) return;
            await SendBatchAsync(batch).ConfigureAwait(false);
        }

        private async Task SendBatchAsync(List<LoggingEvent> events)
        {
            var logs = new List<object>(events.Count);
            foreach (var e in events)
            {
                logs.Add(new
                {
                    timestamp = e.TimeStamp.ToUniversalTime(),
                    level = MapLevel(e.Level),
                    message = e.RenderedMessage,
                    exception = e.ExceptionObject?.ToString(),
                    source = e.LoggerName,
                    properties = (object?)null
                });
            }

            var json = JsonSerializer.Serialize(new { logs });
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl.TrimEnd('/') + "/api/ingest/batch");
                request.Headers.Add("X-API-Key", ApiKey);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await SharedHttpClient.SendAsync(request).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                ErrorHandler?.Error("LogInformant: Failed to send batch", ex);
            }
        }

        private static string MapLevel(Level level)
        {
            if (level == Level.Debug || level == Level.Verbose || level == Level.Trace || level == Level.Fine) return "Debug";
            if (level == Level.Info || level == Level.Notice) return "Information";
            if (level == Level.Warn) return "Warning";
            if (level == Level.Error || level == Level.Severe) return "Error";
            if (level == Level.Fatal || level == Level.Emergency || level == Level.Critical) return "Fatal";
            return "Information";
        }
    }
}
