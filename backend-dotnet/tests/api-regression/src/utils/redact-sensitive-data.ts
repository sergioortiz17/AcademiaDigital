const sensitiveKey = /(authorization|cookie|password|token|secret|api[-_]?key)/i;

function redactUrl(value: string): string {
  try {
    const url = new URL(value, 'http://redaction.local');
    for (const key of [...url.searchParams.keys()]) {
      if (sensitiveKey.test(key)) url.searchParams.set(key, '***REDACTED***');
    }
    return value.startsWith('http') ? url.toString() : `${url.pathname}${url.search}`;
  } catch {
    return value;
  }
}

export function redactSensitiveData(value: unknown, key = ''): unknown {
  if (sensitiveKey.test(key)) return '***REDACTED***';
  if (typeof value === 'string') return key.toLowerCase().includes('url') ? redactUrl(value) : value;
  if (Array.isArray(value)) return value.map((item) => redactSensitiveData(item));
  if (value && typeof value === 'object') {
    return Object.fromEntries(
      Object.entries(value as Record<string, unknown>).map(([entryKey, entryValue]) => [
        entryKey,
        redactSensitiveData(entryValue, entryKey)
      ])
    );
  }
  return value;
}

export function redactHeaders(headers: Record<string, string>): Record<string, unknown> {
  return redactSensitiveData(headers) as Record<string, unknown>;
}
