"""
LogInformant logging handler.

Sends log records to the LogInformant API in batches using a background thread.
No external dependencies — uses the Python standard library only.
"""

import json
import logging
import queue
import threading
import urllib.request
from datetime import datetime, timezone
from typing import Any, Dict, List, Optional

LEVEL_MAP = {
    logging.DEBUG: "Debug",
    logging.INFO: "Information",
    logging.WARNING: "Warning",
    logging.ERROR: "Error",
    logging.CRITICAL: "Fatal",
}


class LogInformantHandler(logging.Handler):
    """
    A logging.Handler that sends log records to LogInformant.

    Records are batched and sent asynchronously to minimise latency impact.

    Example::

        import logging
        from loginformant import LogInformantHandler

        handler = LogInformantHandler(
            api_url="https://app.loginformant.com",
            api_key="YOUR-API-KEY-HERE",
        )
        logging.getLogger().addHandler(handler)
    """

    def __init__(
        self,
        api_url: str,
        api_key: str,
        batch_size: int = 50,
        flush_interval: float = 2.0,
        level: int = logging.NOTSET,
    ) -> None:
        super().__init__(level)
        self._url = api_url.rstrip("/") + "/api/ingest/batch"
        self._api_key = api_key
        self._batch_size = batch_size
        self._flush_interval = flush_interval
        self._queue: queue.Queue = queue.Queue(maxsize=10_000)
        self._stop_event = threading.Event()
        self._thread = threading.Thread(target=self._run, daemon=True, name="LogInformant-Flush")
        self._thread.start()

    # ------------------------------------------------------------------
    # logging.Handler interface
    # ------------------------------------------------------------------

    def emit(self, record: logging.LogRecord) -> None:
        try:
            entry = self._to_entry(record)
            self._queue.put_nowait(entry)
        except queue.Full:
            pass  # drop silently rather than crashing the app

    def close(self) -> None:
        self._stop_event.set()
        self._thread.join(timeout=5)
        self._flush()  # last flush on the calling thread
        super().close()

    # ------------------------------------------------------------------
    # Background flush loop
    # ------------------------------------------------------------------

    def _run(self) -> None:
        while not self._stop_event.wait(timeout=self._flush_interval):
            self._flush()

    def _flush(self) -> None:
        batch: List[Dict[str, Any]] = []
        while len(batch) < self._batch_size:
            try:
                batch.append(self._queue.get_nowait())
            except queue.Empty:
                break
        if batch:
            self._send(batch)

    def _send(self, logs: List[Dict[str, Any]]) -> None:
        payload = json.dumps({"logs": logs}).encode("utf-8")
        req = urllib.request.Request(
            self._url,
            data=payload,
            headers={
                "Content-Type": "application/json",
                "X-API-Key": self._api_key,
            },
            method="POST",
        )
        try:
            with urllib.request.urlopen(req, timeout=10):
                pass
        except Exception:
            pass  # never let logging failures crash the application

    # ------------------------------------------------------------------
    # Record → dict
    # ------------------------------------------------------------------

    def _to_entry(self, record: logging.LogRecord) -> Dict[str, Any]:
        entry: Dict[str, Any] = {
            "timestamp": datetime.fromtimestamp(record.created, tz=timezone.utc).isoformat(),
            "level": LEVEL_MAP.get(record.levelno, "Information"),
            "message": record.getMessage(),
            "source": record.name,
        }
        if record.exc_info:
            import traceback
            entry["exception"] = "".join(traceback.format_exception(*record.exc_info))
        props = {
            k: v
            for k, v in record.__dict__.items()
            if k not in (
                "msg", "args", "levelname", "levelno", "pathname", "filename",
                "module", "exc_info", "exc_text", "stack_info", "lineno",
                "funcName", "created", "msecs", "relativeCreated", "thread",
                "threadName", "processName", "process", "name", "message",
            )
        }
        if props:
            entry["properties"] = props
        return entry
