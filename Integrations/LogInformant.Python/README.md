# loginformant

Send logs to [LogInformant](https://loginformant.com) from **Python 3.8+**.

**Zero external dependencies** — uses only the Python standard library.

## Install

```bash
pip install loginformant
```

## Quick Start

```python
import logging
from loginformant import LogInformantHandler

# Add the handler to the root logger
handler = LogInformantHandler(
    api_url="https://app.loginformant.com",
    api_key="YOUR-API-KEY-HERE",
)
logging.getLogger().addHandler(handler)
logging.getLogger().setLevel(logging.DEBUG)

# Log as normal
logging.info("Server started on port %s", 8080)
logging.warning("High memory usage: %s%%", 87)
logging.error("Database query failed", exc_info=True)
```

## Django

In `settings.py`:

```python
LOGGING = {
    "version": 1,
    "handlers": {
        "loginformant": {
            "class": "loginformant.LogInformantHandler",
            "api_url": "https://app.loginformant.com",
            "api_key": "YOUR-API-KEY-HERE",
        },
    },
    "root": {
        "handlers": ["loginformant"],
        "level": "WARNING",
    },
}
```

## Flask

```python
import logging
from loginformant import LogInformantHandler
from flask import Flask

app = Flask(__name__)

handler = LogInformantHandler(
    api_url="https://app.loginformant.com",
    api_key="YOUR-API-KEY-HERE",
)
app.logger.addHandler(handler)
logging.getLogger("werkzeug").addHandler(handler)
```

## Structured Logging

Attach extra properties to any log message:

```python
logging.info("Order placed", extra={"order_id": 1234, "total": 59.99})
```

## Options

| Parameter        | Default | Description |
|------------------|---------|-------------|
| `api_url`        | —       | Your LogInformant API base URL |
| `api_key`        | —       | API key from your application settings |
| `batch_size`     | 50      | Max logs per HTTP request |
| `flush_interval` | 2.0     | Seconds between automatic flushes |
| `level`          | NOTSET  | Minimum log level to send |

## Log Level Mapping

| Python    | LogInformant |
|-----------|-------------|
| DEBUG     | Debug        |
| INFO      | Information  |
| WARNING   | Warning      |
| ERROR     | Error        |
| CRITICAL  | Fatal        |
