# LogInformant.NLog

Send logs to [LogInformant](https://loginformant.com) using **NLog**. Works with both **.NET Framework 4.8** and **.NET 6+**.

## Install

```powershell
# .NET CLI
dotnet add package LogInformant.NLog

# Package Manager (Visual Studio)
Install-Package LogInformant.NLog
```

## Quick Start — nlog.config

```xml
<?xml version="1.0" encoding="utf-8" ?>
<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd">

  <extensions>
    <add assembly="LogInformant.NLog" />
  </extensions>

  <targets>
    <target xsi:type="LogInformant"
            name="loginformant"
            apiUrl="https://app.loginformant.com"
            apiKey="YOUR-API-KEY-HERE"
            batchSize="50" />
  </targets>

  <rules>
    <logger name="*" minlevel="Debug" writeTo="loginformant" />
  </rules>

</nlog>
```

## Quick Start — Code Configuration

```csharp
using NLog;
using NLog.Config;
using LogInformant.NLog;

var config = new LoggingConfiguration();

var target = new LogInformantTarget
{
    Name = "loginformant",
    ApiUrl = "https://app.loginformant.com",
    ApiKey = "YOUR-API-KEY-HERE"
};

config.AddTarget(target);
config.AddRuleForAllLevels(target);
LogManager.Configuration = config;
```

## Usage

```csharp
private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

Logger.Info("User {0} logged in", userId);
Logger.Warn("Slow query: {0}ms", elapsed);
Logger.Error(ex, "Payment failed for order {0}", orderId);
```

## .NET Framework 4.8 — Web.config

```xml
<configuration>
  <configSections>
    <section name="nlog" type="NLog.Config.ConfigSectionHandler, NLog" />
  </configSections>

  <nlog>
    <extensions>
      <add assembly="LogInformant.NLog" />
    </extensions>
    <targets>
      <target type="LogInformant" name="li"
              apiUrl="https://app.loginformant.com"
              apiKey="YOUR-API-KEY-HERE" />
    </targets>
    <rules>
      <logger name="*" minlevel="Info" writeTo="li" />
    </rules>
  </nlog>
</configuration>
```

## Log Level Mapping

| NLog    | LogInformant |
|---------|-------------|
| Trace   | Debug        |
| Debug   | Debug        |
| Info    | Information  |
| Warn    | Warning      |
| Error   | Error        |
| Fatal   | Fatal        |
