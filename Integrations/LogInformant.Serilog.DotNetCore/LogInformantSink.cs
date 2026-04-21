using Serilog.Debugging;
using Serilog.Events;
using System.Net.Http.Json;
using System.Text.Json;
using PeriodicBatching = Serilog.Sinks.PeriodicBatching;

namespace LogInformant.Serilog.DotNetCore;

public class LogInformantSink : PeriodicBatching.IBatchedLogEventSink
{
    // Static HttpClient with SocketsHttpHandler to prevent socket exhaustion while handling DNS changes.
    // PooledConnectionLifetime ensures connections are recycled to pick up DNS updates.
    private static readonly HttpClient SharedHttpClient = new HttpClient(new SocketsHttpHandler
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(2)
    });

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

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, _apiUrl);
            request.Headers.Add("X-API-Key", _apiKey);
            request.Content = JsonContent.Create(payload);
            var response = await SharedHttpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            SelfLog.WriteLine("[LogInformant] Failed to send batch: {0}", ex.Message);
        }
    }

    public Task OnEmptyBatchAsync() => Task.CompletedTask;

    private static string GetLogLevel(LogEventLevel level) => level switch
    {
        LogEventLevel.Verbose => "Debug",
        LogEventLevel.Debug => "Debug",
        LogEventLevel.Information => "Information",
        LogEventLevel.Warning => "Warning",
        LogEventLevel.Error => "Error",
        LogEventLevel.Fatal => "Fatal",
        _ => "Information"
    };

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
            props[prop.Key] = SerializePropertyValue(prop.Value);
        }
        return props.Count > 0 ? props : null;
    }

    private static object SerializePropertyValue(LogEventPropertyValue value)
    {
        if (value is ScalarValue sv) return sv.Value ?? "null";
        if (value is SequenceValue seq) return seq.Elements.Select(SerializePropertyValue).ToList();
        if (value is StructureValue str)
        {
            var d = new Dictionary<string, object>();
            foreach (var p in str.Properties) d[p.Name] = SerializePropertyValue(p.Value);
            return d;
        }
        if (value is DictionaryValue dict)
        {
            var d = new Dictionary<string, object>();
            foreach (var kvp in dict.Elements) d[SerializePropertyValue(kvp.Key).ToString() ?? "null"] = SerializePropertyValue(kvp.Value);
            return d;
        }
        return value.ToString();
    }
}
