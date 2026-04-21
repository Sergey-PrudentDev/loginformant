export { LogInformantTransport } from './transport';
export interface LogInformantLoggerOptions {
    apiUrl: string;
    apiKey: string;
    batchSize?: number;
    flushIntervalMs?: number;
    defaultProperties?: Record<string, unknown>;
}
export type LogLevel = 'Debug' | 'Information' | 'Warning' | 'Error' | 'Fatal';
export declare class LogInformantLogger {
    private readonly apiUrl;
    private readonly apiKey;
    private readonly batchSize;
    private readonly defaultProperties?;
    private queue;
    private timer;
    constructor(opts: LogInformantLoggerOptions);
    debug(message: string, properties?: Record<string, unknown>): void;
    info(message: string, properties?: Record<string, unknown>): void;
    warn(message: string, properties?: Record<string, unknown>): void;
    error(message: string, error?: Error, properties?: Record<string, unknown>): void;
    fatal(message: string, error?: Error, properties?: Record<string, unknown>): void;
    private log;
    flush(): void;
    dispose(): Promise<void>;
    private sendBatch;
}
//# sourceMappingURL=index.d.ts.map