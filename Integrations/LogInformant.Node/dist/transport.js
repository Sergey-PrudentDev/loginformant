"use strict";
var __importDefault = (this && this.__importDefault) || function (mod) {
    return (mod && mod.__esModule) ? mod : { "default": mod };
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.LogInformantTransport = void 0;
const winston_transport_1 = __importDefault(require("winston-transport"));
const LEVEL_MAP = {
    error: 'Error',
    warn: 'Warning',
    info: 'Information',
    http: 'Information',
    verbose: 'Debug',
    debug: 'Debug',
    silly: 'Debug',
};
class LogInformantTransport extends winston_transport_1.default {
    constructor(opts) {
        var _a, _b;
        super(opts);
        this.queue = [];
        this.timer = null;
        this.apiUrl = opts.apiUrl.replace(/\/$/, '') + '/api/ingest/batch';
        this.apiKey = opts.apiKey;
        this.batchSize = (_a = opts.batchSize) !== null && _a !== void 0 ? _a : 50;
        this.flushIntervalMs = (_b = opts.flushIntervalMs) !== null && _b !== void 0 ? _b : 2000;
        this.timer = setInterval(() => this.flush(), this.flushIntervalMs);
        if (this.timer.unref)
            this.timer.unref(); // don't keep process alive
    }
    log(info, callback) {
        var _a;
        setImmediate(() => this.emit('logged', info));
        const { level, message, stack, ...rest } = info;
        const entry = {
            timestamp: new Date().toISOString(),
            level: (_a = LEVEL_MAP[String(level)]) !== null && _a !== void 0 ? _a : 'Information',
            message: String(message),
            exception: stack ? String(stack) : undefined,
            source: rest.service ? String(rest.service) : undefined,
            properties: Object.keys(rest).length > 0 ? rest : undefined,
        };
        this.queue.push(entry);
        if (this.queue.length >= this.batchSize) {
            this.flush();
        }
        callback();
    }
    flush() {
        if (this.queue.length === 0)
            return;
        const batch = this.queue.splice(0, this.batchSize);
        this.sendBatch(batch).catch(() => { });
    }
    async sendBatch(logs) {
        try {
            await fetch(this.apiUrl, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-API-Key': this.apiKey,
                },
                body: JSON.stringify({ logs }),
            });
        }
        catch (_a) {
            // silently swallow — logging should never crash the app
        }
    }
    close() {
        if (this.timer) {
            clearInterval(this.timer);
            this.timer = null;
        }
        this.flush();
    }
}
exports.LogInformantTransport = LogInformantTransport;
//# sourceMappingURL=transport.js.map