# @loginformant/node

Send logs to [LogInformant](https://loginformant.com) from **Node.js**. Includes a **Winston transport** and a **standalone logger** (no dependencies required).

## Install

```bash
npm install @loginformant/node
```

## Option A — Winston Transport

```bash
npm install winston @loginformant/node
```

```javascript
const winston = require('winston');
const { LogInformantTransport } = require('@loginformant/node');

const logger = winston.createLogger({
  transports: [
    new winston.transports.Console(),
    new LogInformantTransport({
      apiUrl: 'https://app.loginformant.com',
      apiKey: 'YOUR-API-KEY-HERE',
    }),
  ],
});

logger.info('Server started', { port: 3000 });
logger.error('Database connection failed', { host: 'db.example.com' });
```

## Option B — Standalone Logger (no deps)

```javascript
const { LogInformantLogger } = require('@loginformant/node');

const log = new LogInformantLogger({
  apiUrl: 'https://app.loginformant.com',
  apiKey: 'YOUR-API-KEY-HERE',
  defaultProperties: { service: 'api', version: '1.2.0' },
});

log.info('User signed up', { userId: 42 });
log.warn('Slow query', { queryMs: 1200 });
log.error('Payment failed', new Error('Card declined'), { orderId: 99 });

// Flush before process exit
process.on('beforeExit', async () => { await log.dispose(); });
```

## TypeScript

```typescript
import { LogInformantTransport, LogInformantLogger } from '@loginformant/node';
```

## Options

| Option           | Default | Description |
|------------------|---------|-------------|
| `apiUrl`         | —       | Your LogInformant API URL |
| `apiKey`         | —       | API key from your application settings |
| `batchSize`      | 50      | Max logs per HTTP request |
| `flushIntervalMs`| 2000    | How often to flush (milliseconds) |

## Log Level Mapping (Winston)

| Winston  | LogInformant |
|----------|-------------|
| error    | Error        |
| warn     | Warning      |
| info     | Information  |
| http     | Information  |
| verbose  | Debug        |
| debug    | Debug        |
| silly    | Debug        |
