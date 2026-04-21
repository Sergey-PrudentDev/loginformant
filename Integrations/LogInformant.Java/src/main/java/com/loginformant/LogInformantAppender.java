package com.loginformant;

import ch.qos.logback.classic.spi.ILoggingEvent;
import ch.qos.logback.classic.spi.IThrowableProxy;
import ch.qos.logback.core.AppenderBase;
import com.fasterxml.jackson.databind.ObjectMapper;

import java.net.URI;
import java.net.http.HttpClient;
import java.net.http.HttpRequest;
import java.net.http.HttpResponse;
import java.time.Duration;
import java.time.Instant;
import java.util.*;
import java.util.concurrent.*;

/**
 * Logback appender that batches log events and sends them to LogInformant.
 *
 * <p>Configure in logback.xml:
 * <pre>{@code
 * <appender name="LOGINFORMANT" class="com.loginformant.LogInformantAppender">
 *   <apiUrl>https://app.loginformant.com</apiUrl>
 *   <apiKey>YOUR-API-KEY-HERE</apiKey>
 * </appender>
 * }</pre>
 */
public class LogInformantAppender extends AppenderBase<ILoggingEvent> {

    private static final HttpClient HTTP_CLIENT = HttpClient.newBuilder()
            .connectTimeout(Duration.ofSeconds(5))
            .build();
    private static final ObjectMapper MAPPER = new ObjectMapper();

    private String apiUrl;
    private String apiKey;
    private int batchSize = 50;
    private int flushIntervalMs = 2000;

    private final BlockingQueue<Map<String, Object>> queue = new LinkedBlockingQueue<>(10_000);
    private ScheduledExecutorService scheduler;

    // -----------------------------------------------------------------
    // Logback lifecycle
    // -----------------------------------------------------------------

    @Override
    public void start() {
        if (apiUrl == null || apiUrl.isBlank()) {
            addError("LogInformant: apiUrl is required");
            return;
        }
        if (apiKey == null || apiKey.isBlank()) {
            addError("LogInformant: apiKey is required");
            return;
        }
        scheduler = Executors.newSingleThreadScheduledExecutor(r -> {
            Thread t = new Thread(r, "LogInformant-Flush");
            t.setDaemon(true);
            return t;
        });
        scheduler.scheduleAtFixedRate(this::flush, flushIntervalMs, flushIntervalMs, TimeUnit.MILLISECONDS);
        super.start();
    }

    @Override
    public void stop() {
        if (scheduler != null) {
            scheduler.shutdown();
        }
        flush(); // final flush
        super.stop();
    }

    // -----------------------------------------------------------------
    // AppenderBase interface
    // -----------------------------------------------------------------

    @Override
    protected void append(ILoggingEvent event) {
        Map<String, Object> entry = new LinkedHashMap<>();
        entry.put("timestamp", Instant.ofEpochMilli(event.getTimeStamp()).toString());
        entry.put("level", mapLevel(event.getLevel().levelStr));
        entry.put("message", event.getFormattedMessage());
        entry.put("source", event.getLoggerName());

        IThrowableProxy tp = event.getThrowableProxy();
        if (tp != null) {
            entry.put("exception", formatThrowable(tp));
        }

        Map<String, String> mdc = event.getMDCPropertyMap();
        if (mdc != null && !mdc.isEmpty()) {
            entry.put("properties", new LinkedHashMap<>(mdc));
        }

        queue.offer(entry);
    }

    // -----------------------------------------------------------------
    // Batch flush
    // -----------------------------------------------------------------

    private void flush() {
        List<Map<String, Object>> batch = new ArrayList<>(batchSize);
        queue.drainTo(batch, batchSize);
        if (batch.isEmpty()) return;

        try {
            String body = MAPPER.writeValueAsString(Map.of("logs", batch));
            String url = apiUrl.replaceAll("/+$", "") + "/api/ingest/batch";
            HttpRequest request = HttpRequest.newBuilder()
                    .uri(URI.create(url))
                    .header("Content-Type", "application/json")
                    .header("X-API-Key", apiKey)
                    .POST(HttpRequest.BodyPublishers.ofString(body))
                    .timeout(Duration.ofSeconds(10))
                    .build();
            HTTP_CLIENT.sendAsync(request, HttpResponse.BodyHandlers.discarding());
        } catch (Exception e) {
            addWarn("LogInformant: Failed to send batch", e);
        }
    }

    private static String mapLevel(String level) {
        return switch (level.toUpperCase()) {
            case "TRACE", "DEBUG" -> "Debug";
            case "INFO"            -> "Information";
            case "WARN"            -> "Warning";
            case "ERROR"           -> "Error";
            default                -> "Fatal";
        };
    }

    private static String formatThrowable(IThrowableProxy tp) {
        StringBuilder sb = new StringBuilder(tp.getClassName()).append(": ").append(tp.getMessage());
        for (var ste : tp.getStackTraceElementProxyArray()) {
            sb.append("\n\tat ").append(ste.getSTEAsString());
        }
        return sb.toString();
    }

    // -----------------------------------------------------------------
    // Setters (called by Logback from XML config)
    // -----------------------------------------------------------------

    public void setApiUrl(String apiUrl)             { this.apiUrl = apiUrl; }
    public void setApiKey(String apiKey)             { this.apiKey = apiKey; }
    public void setBatchSize(int batchSize)          { this.batchSize = batchSize; }
    public void setFlushIntervalMs(int ms)           { this.flushIntervalMs = ms; }
}
