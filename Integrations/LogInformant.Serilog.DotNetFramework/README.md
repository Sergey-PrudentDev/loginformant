# LogInformant.Serilog.DotNetFramework

Send logs to [LogInformant](https://loginformant.com) from **.NET Framework 4.8** applications using Serilog.

> **Using .NET 6+?** Use [LogInformant.Serilog.DotNetCore](../LogInformant.Serilog.DotNetCore) instead.

## Install

```powershell
Install-Package LogInformant.Serilog.DotNetFramework
```

## Quick Start — Global.asax.cs (one line)

```csharp
using Serilog;
using LogInformant.Serilog.DotNetFramework;

public class MvcApplication : System.Web.HttpApplication
{
    protected void Application_Start()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.LogInformant(
                apiUrl: "https://app.loginformant.com",
                apiKey: System.Configuration.ConfigurationManager.AppSettings["LogInformant:ApiKey"])
            .CreateLogger();

        // rest of your startup...
        AreaRegistration.RegisterAllAreas();
        FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
        RouteConfig.RegisterRoutes(RouteTable.Routes);
    }

    protected void Application_End()
    {
        Log.CloseAndFlush(); // flush remaining logs before shutdown
    }
}
```

**Web.config** — store your API key here:
```xml
<configuration>
  <appSettings>
    <add key="LogInformant:ApiKey" value="YOUR-API-KEY-HERE" />
  </appSettings>
</configuration>
```

## Configure via Web.config (Serilog.Settings.AppSettings)

Install the additional package:
```powershell
Install-Package Serilog.Settings.AppSettings
```

**Global.asax.cs** — single line:
```csharp
Log.Logger = new LoggerConfiguration()
    .ReadFrom.AppSettings()
    .CreateLogger();
```

**Web.config:**
```xml
<appSettings>
  <add key="serilog:using:LogInformant"            value="LogInformant.Serilog.DotNetFramework" />
  <add key="serilog:write-to:LogInformant.apiUrl"  value="https://app.loginformant.com" />
  <add key="serilog:write-to:LogInformant.apiKey"  value="YOUR-API-KEY-HERE" />
</appSettings>
```

## Usage

### Static logger (simplest)
```csharp
using Serilog;

Log.Information("User {UserId} logged in", userId);
Log.Warning("Slow query detected: {QueryMs}ms", elapsed);
Log.Error(ex, "Payment failed for order {OrderId}", orderId);
```

### ILogger via dependency injection
If your app uses a DI container (Unity, Autofac, etc.) you can wire up `ILogger<T>`:

```csharp
// With Autofac
builder.Register(c => new SerilogLoggerFactory(Log.Logger))
       .As<Microsoft.Extensions.Logging.ILoggerFactory>();
builder.RegisterGeneric(typeof(Logger<>))
       .As(typeof(Microsoft.Extensions.Logging.ILogger<>));
```

Then inject normally:
```csharp
public class PaymentService
{
    private readonly ILogger<PaymentService> _logger;
    public PaymentService(ILogger<PaymentService> logger) => _logger = logger;
}
```

## Log Level Mapping

| Serilog     | LogInformant |
|-------------|-------------|
| Verbose     | Debug        |
| Debug       | Debug        |
| Information | Information  |
| Warning     | Warning      |
| Error       | Error        |
| Fatal       | Fatal        |
