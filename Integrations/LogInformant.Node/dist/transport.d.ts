import Transport from 'winston-transport';
interface LogInformantTransportOptions extends Transport.TransportStreamOptions {
    apiUrl: string;
    apiKey: string;
    batchSize?: number;
    flushIntervalMs?: number;
}
export declare class LogInformantTransport extends Transport {
    private readonly apiUrl;
    private readonly apiKey;
    private readonly batchSize;
    private readonly flushIntervalMs;
    private queue;
    private timer;
    constructor(opts: LogInformantTransportOptions);
    log(info: Record<string, unknown>, callback: () => void): void;
    private flush;
    private sendBatch;
    close(): void;
}
export {};
//# sourceMappingURL=transport.d.ts.map