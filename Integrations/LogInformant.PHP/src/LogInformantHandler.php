<?php

declare(strict_types=1);

namespace LogInformant;

use Monolog\Handler\AbstractProcessingHandler;
use Monolog\Level;
use Monolog\LogRecord;

/**
 * Monolog handler that sends log records to the LogInformant API.
 *
 * Records are buffered and flushed in batches to reduce HTTP overhead.
 */
class LogInformantHandler extends AbstractProcessingHandler
{
    private string $apiUrl;
    private string $apiKey;
    private int $batchSize;
    private array $buffer = [];

    private const LEVEL_MAP = [
        Level::Debug->value     => 'Debug',
        Level::Info->value      => 'Information',
        Level::Notice->value    => 'Information',
        Level::Warning->value   => 'Warning',
        Level::Error->value     => 'Error',
        Level::Critical->value  => 'Fatal',
        Level::Alert->value     => 'Fatal',
        Level::Emergency->value => 'Fatal',
    ];

    public function __construct(
        string $apiUrl,
        string $apiKey,
        int $batchSize = 50,
        int|string|Level $level = Level::Debug,
        bool $bubble = true
    ) {
        parent::__construct($level, $bubble);
        $this->apiUrl    = rtrim($apiUrl, '/') . '/api/ingest/batch';
        $this->apiKey    = $apiKey;
        $this->batchSize = $batchSize;
    }

    protected function write(LogRecord $record): void
    {
        $this->buffer[] = [
            'timestamp'  => $record->datetime->format('c'),
            'level'      => self::LEVEL_MAP[$record->level->value] ?? 'Information',
            'message'    => $record->message,
            'exception'  => isset($record->context['exception'])
                                ? (string) $record->context['exception']
                                : null,
            'source'     => $record->channel,
            'properties' => $record->context ?: null,
        ];

        if (count($this->buffer) >= $this->batchSize) {
            $this->flush();
        }
    }

    public function flush(): void
    {
        if (empty($this->buffer)) {
            return;
        }

        $logs   = $this->buffer;
        $this->buffer = [];

        $payload = json_encode(['logs' => $logs], JSON_UNESCAPED_UNICODE | JSON_THROW_ON_ERROR);

        $ch = curl_init($this->apiUrl);
        curl_setopt_array($ch, [
            CURLOPT_POST           => true,
            CURLOPT_POSTFIELDS     => $payload,
            CURLOPT_RETURNTRANSFER => true,
            CURLOPT_TIMEOUT        => 10,
            CURLOPT_HTTPHEADER     => [
                'Content-Type: application/json',
                'X-API-Key: ' . $this->apiKey,
            ],
        ]);
        curl_exec($ch);
        curl_close($ch);
    }

    public function __destruct()
    {
        $this->flush();
    }
}
