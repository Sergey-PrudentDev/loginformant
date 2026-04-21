import Transport from 'winston-transport';

interface LogInformantTransportOptions extends Transport.TransportStreamOptions {
  apiUrl: string;
  apiKey: string;
  batchSize?: number;
  flushIntervalMs?: number;
}

interface LogEntry {
  timestamp: string;
  level: string;
  message: string;
  exception?: string;
  source?: string;
  properties?: Record<string, unknown>;
}

const LEVEL_MAP: Record<string, string> = {
  error: 'Error',
  warn: 'Warning',
  info: 'Information',
  http: 'Information',
  verbose: 'Debug',
  debug: 'Debug',
  silly: 'Debug',
};

export class LogInformantTransport extends Transport {
  private readonly apiUrl: string;
  private readonly apiKey: string;
  private readonly batchSize: number;
  private readonly flushIntervalMs: number;
  private queue: LogEntry[] = [];
  private timer: ReturnType<typeof setInterval> | null = null;

  constructor(opts: LogInformantTransportOptions) {
    super(opts);
    this.apiUrl = opts.apiUrl.replace(/\/$/, '') + '/api/ingest/batch';
    this.apiKey = opts.apiKey;
    this.batchSize = opts.batchSize ?? 50;
    this.flushIntervalMs = opts.flushIntervalMs ?? 2000;
    this.timer = setInterval(() => this.flush(), this.flushIntervalMs);
    if (this.timer.unref) this.timer.unref(); // don't keep process alive
  }

  log(info: Record<string, unknown>, callback: () => void): void {
    setImmediate(() => this.emit('logged', info));

    const { level, message, stack, ...rest } = info;

    const entry: LogEntry = {
      timestamp: new Date().toISOString(),
      level: LEVEL_MAP[String(level)] ?? 'Information',
      message: String(message),
      exception: stack ? String(stack) : undefined,
      source: rest.service ? String(rest.service) : undefined,
      properties: Object.keys(rest).length > 0 ? rest as Record<string, unknown> : undefined,
    };

    this.queue.push(entry);
    if (this.queue.length >= this.batchSize) {
      this.flush();
    }

    callback();
  }

  private flush(): void {
    if (this.queue.length === 0) return;
    const batch = this.queue.splice(0, this.batchSize);
    this.sendBatch(batch).catch(() => {});
  }

  private async sendBatch(logs: LogEntry[]): Promise<void> {
    try {
      await fetch(this.apiUrl, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-API-Key': this.apiKey,
        },
        body: JSON.stringify({ logs }),
      });
    } catch {
      // silently swallow — logging should never crash the app
    }
  }

  close(): void {
    if (this.timer) {
      clearInterval(this.timer);
      this.timer = null;
    }
    this.flush();
  }
}
