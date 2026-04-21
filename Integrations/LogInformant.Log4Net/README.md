# LogInformant.Log4Net

Send logs to [LogInformant](https://loginformant.com) using **log4net**. Works with both **.NET Framework 4.8** and **.NET 6+**.

## Install

```powershell
# .NET CLI
dotnet add package LogInformant.Log4Net

# Package Manager (Visual Studio)
Install-Package LogInformant.Log4Net
```

## Quick Start — log4net.config

```xml
<?xml version="1.0" encoding="utf-8" ?>
<log4net>
  <appender name="LogInformant" type="LogInformant.Log4Net.LogInformantAppender, LogInformant.Log4Net">
    <apiUrl value="https://app.loginformant.com" />
    <apiKey value="YOUR-API-KEY-HERE" />
    <batchSize value="50" />
  </appender>

  <root>
    <level value="DEBUG" />
    <appender-ref ref="LogInformant" />
  </root>
</log4net>
```

## .NET Framework 4.8 — Web.config

Add to your `web.config`:
```xml
<configuration>
  <configSections>
    <section name="log4net" type="log4net.Config.Log4NetConfigurationSectionHandler, log4net" />
  </configSections>

  <log4net>
    <appender name="LogInformant" type="LogInformant.Log4Net.LogInformantAppender, LogInformant.Log4Net">
      <apiUrl value="https://app.loginformant.com" />
      <apiKey value="YOUR-API-KEY-HERE" />
    </appender>
    <root>
      <level value="INFO" />
      <appender-ref ref="LogInformant" />
    </root>
  </log4net>
</configuration>
```

In `Global.asax.cs` or `AssemblyInfo.cs`:
```csharp
[assembly: log4net.Config.XmlConfigurator(Watch = true)]
```

## Usage

```csharp
using log4net;

private static readonly ILog Log = LogManager.GetLogger(typeof(MyClass));

Log.Info("User logged in");
Log.Warn("Slow query detected");
Log.Error("Payment failed", ex);
```

## Code-Only Setup (.NET 6+)

```csharp
using log4net;
using log4net.Repository.Hierarchy;
using LogInformant.Log4Net;

var hierarchy = (Hierarchy)LogManager.GetRepository();
var appender = new LogInformantAppender
{
    ApiUrl = "https://app.loginformant.com",
    ApiKey = "YOUR-API-KEY-HERE"
};
appender.ActivateOptions();
hierarchy.Root.AddAppender(appender);
hierarchy.Root.Level = log4net.Core.Level.Debug;
hierarchy.Configured = true;
```

## Log Level Mapping

| log4net | LogInformant |
|---------|-------------|
| DEBUG   | Debug        |
| INFO    | Information  |
| WARN    | Warning      |
| ERROR   | Error        |
| FATAL   | Fatal        |
