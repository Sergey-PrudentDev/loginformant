# loginformant/monolog-handler

Send logs to [LogInformant](https://loginformant.com) from **PHP** using **Monolog v3**.

## Install

```bash
composer require loginformant/monolog-handler
```

## Quick Start

```php
<?php

use Monolog\Logger;
use LogInformant\LogInformantHandler;

$log = new Logger('myapp');
$log->pushHandler(new LogInformantHandler(
    apiUrl: 'https://app.loginformant.com',
    apiKey: 'YOUR-API-KEY-HERE',
));

$log->info('User logged in', ['user_id' => 42]);
$log->warning('Slow query', ['query_ms' => 1200]);
$log->error('Payment failed', ['order_id' => 99, 'exception' => $e]);
```

## Laravel

In `config/logging.php`:

```php
'channels' => [
    'loginformant' => [
        'driver'  => 'monolog',
        'handler' => \LogInformant\LogInformantHandler::class,
        'with'    => [
            'apiUrl' => env('LOGINFORMANT_URL', 'https://app.loginformant.com'),
            'apiKey' => env('LOGINFORMANT_KEY'),
        ],
    ],
],
```

In `.env`:
```
LOGINFORMANT_KEY=YOUR-API-KEY-HERE
```

Set the default channel: `LOG_CHANNEL=loginformant`

## Symfony

In `config/packages/monolog.yaml`:

```yaml
monolog:
  handlers:
    loginformant:
      type: service
      id: LogInformant\LogInformantHandler
```

In `services.yaml`:

```yaml
LogInformant\LogInformantHandler:
  arguments:
    $apiUrl: 'https://app.loginformant.com'
    $apiKey: '%env(LOGINFORMANT_KEY)%'
```

## Options

| Parameter   | Default | Description |
|-------------|---------|-------------|
| `apiUrl`    | —       | Your LogInformant API base URL |
| `apiKey`    | —       | API key from your application settings |
| `batchSize` | 50      | Flush after this many records |
| `level`     | DEBUG   | Minimum Monolog level to handle |

## Log Level Mapping

| Monolog   | LogInformant |
|-----------|-------------|
| DEBUG     | Debug        |
| INFO      | Information  |
| NOTICE    | Information  |
| WARNING   | Warning      |
| ERROR     | Error        |
| CRITICAL  | Fatal        |
| ALERT     | Fatal        |
| EMERGENCY | Fatal        |
