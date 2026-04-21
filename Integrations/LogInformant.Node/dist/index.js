"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.LogInformantLogger = exports.LogInformantTransport = void 0;
var transport_1 = require("./transport");
Object.defineProperty(exports, "LogInformantTransport", { enumerable: true, get: function () { return transport_1.LogInformantTransport; } });
class LogInformantLogger {
    constructor(opts) {
        var _a, _b;
        this.queue = [];
        this.apiUrl = opts.apiUrl.replace(/\/$/, '') + '/api/ingest/batch';
        this.apiKey = opts.apiKey;
        this.batchSize = (_a = opts.batchSize) !== null && _a !== void 0 ? _a : 50;
        this.defaultProperties = opts.defaultProperties;
        this.timer = setInterval(() => this.flush(), (_b = opts.flushIntervalMs) !== null && _b !== void 0 ? _b : 2000);
        if (this.timer.unref)
            this.timer.unref();
    }
    debug(message, properties) {
        this.log('Debug', message, undefined, properties);
    }
    info(message, properties) {
        this.log('Information', message, undefined, properties);
    }
    warn(message, properties) {
        this.log('Warning', message, undefined, properties);
    }
    error(message, error, properties) {
        this.log('Error', message, error === null || error === void 0 ? void 0 : error.stack, properties);
    }
    fatal(message, error, properties) {
        this.log('Fatal', message, error === null || error === void 0 ? void 0 : error.stack, properties);
    }
    log(level, message, exception, properties) {
        const merged = { ...this.defaultProperties, ...properties };
        this.queue.push({
            timestamp: new Date().toISOString(),
            level,
            message,
            exception,
            properties: Object.keys(merged).length > 0 ? merged : undefined,
        });
        if (this.queue.length >= this.batchSize)
            this.flush();
    }
    flush() {
        if (this.queue.length === 0)
            return;
        const batch = this.queue.splice(0, this.batchSize);
        this.sendBatch(batch).catch(() => { });
    }
    async dispose() {
        clearInterval(this.timer);
        this.flush();
        await new Promise(r => setTimeout(r, 500)); // give final batch time to send
    }
    async sendBatch(logs) {
        try {
            await fetch(this.apiUrl, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json', 'X-API-Key': this.apiKey },
                body: JSON.stringify({ logs }),
            });
        }
        catch (_a) {
            // silently swallow
        }
    }
}
exports.LogInformantLogger = LogInformantLogger;
//# sourceMappingURL=index.js.map