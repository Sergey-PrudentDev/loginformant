# LogInformant Logback Appender (Java)

Send logs to [LogInformant](https://loginformant.com) from **Java 11+** using **Logback / SLF4J**.

## Add to your project

**Maven (`pom.xml`)**
```xml
<dependency>
  <groupId>com.loginformant</groupId>
  <artifactId>logback-appender</artifactId>
  <version>1.0.0</version>
</dependency>
```

**Gradle (`build.gradle`)**
```groovy
implementation 'com.loginformant:logback-appender:1.0.0'
```

## Configure (`logback.xml`)

```xml
<?xml version="1.0" encoding="UTF-8"?>
<configuration>

  <appender name="LOGINFORMANT" class="com.loginformant.LogInformantAppender">
    <apiUrl>https://app.loginformant.com</apiUrl>
    <apiKey>YOUR-API-KEY-HERE</apiKey>
    <!-- Optional -->
    <batchSize>50</batchSize>
    <flushIntervalMs>2000</flushIntervalMs>
  </appender>

  <!-- Keep console output during development -->
  <appender name="CONSOLE" class="ch.qos.logback.core.ConsoleAppender">
    <encoder>
      <pattern>%d{HH:mm:ss} %-5level %logger{36} - %msg%n</pattern>
    </encoder>
  </appender>

  <root level="INFO">
    <appender-ref ref="LOGINFORMANT" />
    <appender-ref ref="CONSOLE" />
  </root>

</configuration>
```

## Usage (SLF4J)

```java
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

public class OrderService {
    private static final Logger log = LoggerFactory.getLogger(OrderService.class);

    public void placeOrder(int orderId) {
        log.info("Order {} placed", orderId);

        try {
            // process...
        } catch (Exception e) {
            log.error("Failed to place order {}", orderId, e);
        }
    }
}
```

## Spring Boot

Add `logback-spring.xml` to `src/main/resources/`:

```xml
<?xml version="1.0" encoding="UTF-8"?>
<configuration>
  <springProperty name="LI_API_KEY" source="loginformant.api-key"/>

  <appender name="LOGINFORMANT" class="com.loginformant.LogInformantAppender">
    <apiUrl>https://app.loginformant.com</apiUrl>
    <apiKey>${LI_API_KEY}</apiKey>
  </appender>

  <root level="INFO">
    <appender-ref ref="LOGINFORMANT" />
  </root>
</configuration>
```

In `application.properties`:
```properties
loginformant.api-key=YOUR-API-KEY-HERE
```

## MDC (Structured Context)

```java
import org.slf4j.MDC;

MDC.put("userId", String.valueOf(userId));
MDC.put("requestId", requestId);
log.info("Processing payment");
MDC.clear();
```

All MDC values are sent as `properties` to LogInformant.

## Log Level Mapping

| Logback | LogInformant |
|---------|-------------|
| TRACE   | Debug        |
| DEBUG   | Debug        |
| INFO    | Information  |
| WARN    | Warning      |
| ERROR   | Error        |
| ERROR (fatal) | Fatal |
