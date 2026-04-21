# LogInformant.Serilog.DotNetCore

Send logs to [LogInformant](https://loginformant.com) from any **.NET 6+ / ASP.NET Core** application using Serilog.

> **Using .NET Framework 4.8?** Use [LogInformant.Serilog.DotNetFramework](../LogInformant.Serilog.DotNetFramework) instead.

## Install

```bash
dotnet add package LogInformant.Serilog.DotNetCore
```

## Quick Start (ASP.NET Core)

**Program.cs**
```csharp
using LogInformant.Serilog.DotNetCore;

builder.Host.UseSerilog((ctx, cfg) =>
    cfg.ReadFrom.Configuration(ctx.Configuration)
       .WriteTo.LogInformant(
           apiUrl: "https://app.loginformant.com",
           apiKey: ctx.Configuration["LogInformant:ApiKey"]!));
```

**appsettings.json**
```json
{
  "LogInformant": {
    "ApiKey": "YOUR-API-KEY-HERE"
  }
}
```

That's it. Your logs appear in LogInformant within seconds.

## Usage

Inject `ILogger<T>` as normal — logs flow automatically:

```csharp
public class OrderService(ILogger<OrderService> logger)
{
    public void PlaceOrder(int orderId)
    {
        logger.LogInformation("Order {OrderId} placed", orderId);

        try { /* process */ }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to place order {OrderId}", orderId);
            throw;
        }
    }
}
```

## Code-Only Setup (Console Apps, Workers)

```csharp
using Serilog;
using LogInformant.Serilog.DotNetCore;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .WriteTo.LogInformant(
        apiUrl: "https://app.loginformant.com",
        apiKey: "YOUR-API-KEY-HERE")
    .CreateLogger();

Log.Information("Worker started");
// ...
Log.CloseAndFlush(); // flush before process exit
```

## Multiple Environments

Use a different API key per environment — each key maps to a named environment in LogInformant:

```json
{
  "LogInformant": {
    "ApiKey": "YOUR-PROD-KEY"
  }
}
```

```json
// appsettings.Development.json
{
  "LogInformant": {
    "ApiKey": "YOUR-DEV-KEY"
  }
}
```

## Options

| Parameter     | Default | Description |
|---------------|---------|-------------|
| `apiUrl`      | —       | Your LogInformant API base URL |
| `apiKey`      | —       | API key from your application's settings page |
| `batchSize`   | 50      | Maximum logs per HTTP request |
| `period`      | 2s      | How often to flush the batch |

## Log Level Mapping

| Serilog       | LogInformant |
|---------------|-------------|
| Verbose       | Debug        |
| Debug         | Debug        |
| Information   | Information  |
| Warning       | Warning      |
| Error         | Error        |
| Fatal         | Fatal        |
