export { LogInformantTransport } from './transport';

// Standalone logger for apps not using Winston
export interface LogInformantLoggerOptions {
  apiUrl: string;
  apiKey: string;
  batchSize?: number;
  flushIntervalMs?: number;
  defaultProperties?: Record<string, unknown>;
}

export type LogLevel = 'Debug' | 'Information' | 'Warning' | 'Error' | 'Fatal';

interface LogEntry {
  timestamp: string;
  level: string;
  message: string;
  exception?: string;
  source?: string;
  properties?: Record<string, unknown>;
}

export class LogInformantLogger {
  private readonly apiUrl: string;
  private readonly apiKey: string;
  private readonly batchSize: number;
  private readonly defaultProperties?: Record<string, unknown>;
  private queue: LogEntry[] = [];
  private timer: ReturnType<typeof setInterval>;

  constructor(opts: LogInformantLoggerOptions) {
    this.apiUrl = opts.apiUrl.replace(/\/$/, '') + '/api/ingest/batch';
    this.apiKey = opts.apiKey;
    this.batchSize = opts.batchSize ?? 50;
    this.defaultProperties = opts.defaultProperties;

    this.timer = setInterval(() => this.flush(), opts.flushIntervalMs ?? 2000);
    if ((this.timer as any).unref) (this.timer as any).unref();
  }

  debug(message: string, properties?: Record<string, unknown>): void {
    this.log('Debug', message, undefined, properties);
  }

  info(message: string, properties?: Record<string, unknown>): void {
    this.log('Information', message, undefined, properties);
  }

  warn(message: string, properties?: Record<string, unknown>): void {
    this.log('Warning', message, undefined, properties);
  }

  error(message: string, error?: Error, properties?: Record<string, unknown>): void {
    this.log('Error', message, error?.stack, properties);
  }

  fatal(message: string, error?: Error, properties?: Record<string, unknown>): void {
    this.log('Fatal', message, error?.stack, properties);
  }

  private log(level: string, message: string, exception?: string, properties?: Record<string, unknown>): void {
    const merged = { ...this.defaultProperties, ...properties };
    this.queue.push({
      timestamp: new Date().toISOString(),
      level,
      message,
      exception,
      properties: Object.keys(merged).length > 0 ? merged : undefined,
    });
    if (this.queue.length >= this.batchSize) this.flush();
  }

  flush(): void {
    if (this.queue.length === 0) return;
    const batch = this.queue.splice(0, this.batchSize);
    this.sendBatch(batch).catch(() => {});
  }

  async dispose(): Promise<void> {
    clearInterval(this.timer);
    this.flush();
    await new Promise(r => setTimeout(r, 500)); // give final batch time to send
  }

  private async sendBatch(logs: LogEntry[]): Promise<void> {
    try {
      await fetch(this.apiUrl, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'X-API-Key': this.apiKey },
        body: JSON.stringify({ logs }),
      });
    } catch {
      // silently swallow
    }
  }
}
