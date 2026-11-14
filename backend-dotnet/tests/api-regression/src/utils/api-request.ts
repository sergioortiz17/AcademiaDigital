import { APIRequestContext, APIResponse } from '@playwright/test';
import * as allure from 'allure-js-commons';
import { redactHeaders, redactSensitiveData } from './redact-sensitive-data';

export type HttpMethod = 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE';

export interface ApiRequestOptions {
  operation: string;
  method: HttpMethod;
  path: string;
  query?: Record<string, string | number | boolean | undefined>;
  headers?: Record<string, string>;
  body?: unknown;
  rawData?: string;
}

export interface ApiCallResult<T = unknown> {
  response: APIResponse;
  body: T;
  rawBody: Buffer;
  durationMs: number;
}

async function attachJson(name: string, value: unknown): Promise<void> {
  await allure.attachment(name, JSON.stringify(value, null, 2), {
    contentType: 'application/json',
    fileExtension: 'json'
  });
}

function normalizedQuery(query: ApiRequestOptions['query']): Record<string, string | number | boolean> {
  const normalized = Object.fromEntries(Object.entries(query ?? {}).filter((entry): entry is [string, string | number | boolean] => entry[1] !== undefined));
  if ('commissionId' in normalized && !('divisionId' in normalized))
    normalized.divisionId = normalized.commissionId;
  return normalized;
}

function addCurrentAcademicNames(value: unknown): unknown {
  if (Array.isArray(value)) return value.map(addCurrentAcademicNames);
  if (typeof value !== 'object' || value === null) return value;
  const result: Record<string, unknown> = Object.fromEntries(
    Object.entries(value).map(([key, item]) => [key, addCurrentAcademicNames(item)])
  );
  if ('commissionId' in result && !('divisionId' in result)) result.divisionId = result.commissionId;
  if ('teachingPositionId' in result && !('courseSectionId' in result)) result.courseSectionId = result.teachingPositionId;
  if (typeof result.shift === 'string')
    result.shift = ({ Morning: 'Mañana', Afternoon: 'Tarde', Evening: 'Noche' } as Record<string, string>)[result.shift] ?? result.shift;
  return result;
}

function addLegacyAcademicNames(value: unknown): unknown {
  if (Array.isArray(value)) return value.map(addLegacyAcademicNames);
  if (typeof value !== 'object' || value === null) return value;
  const result: Record<string, unknown> = Object.fromEntries(
    Object.entries(value).map(([key, item]) => [key, addLegacyAcademicNames(item)])
  );
  if ('divisionId' in result && !('commissionId' in result)) result.commissionId = result.divisionId;
  if ('divisionCode' in result && !('commissionCode' in result)) result.commissionCode = result.divisionCode;
  if ('divisionName' in result && !('commissionName' in result)) result.commissionName = result.divisionName;
  if ('courseSectionId' in result && !('teachingPositionId' in result)) result.teachingPositionId = result.courseSectionId;
  if (typeof result.shift === 'string')
    result.shift = ({ 'Mañana': 'Morning', Tarde: 'Afternoon', Noche: 'Evening' } as Record<string, string>)[result.shift] ?? result.shift;
  return result;
}

export class ApiRequestExecutor {
  constructor(
    private readonly context: APIRequestContext,
    private readonly defaultHeaders: Record<string, string> = {}
  ) {}

  async send<T = unknown>(options: ApiRequestOptions): Promise<ApiCallResult<T>> {
    return allure.step(options.operation, async () => {
      const query = normalizedQuery(options.query);
      const headers = { ...this.defaultHeaders, ...options.headers };
      const url = new URL(options.path, 'http://request.local');
      for (const [key, value] of Object.entries(query)) url.searchParams.set(key, String(value));

      await attachJson('request-headers.json', redactHeaders(headers));
      await attachJson('request-query-parameters.json', redactSensitiveData(query));
      const requestBody = options.rawData ?? addCurrentAcademicNames(options.body);
      await attachJson('request-body.json', redactSensitiveData(requestBody ?? null));

      const timestamp = new Date().toISOString();
      const startedAt = Date.now();
      try {
        const response = await this.context.fetch(options.path, {
          method: options.method,
          params: query,
          headers,
          data: requestBody,
          failOnStatusCode: false
        });
        const durationMs = Date.now() - startedAt;
        const rawBody = await response.body();
        const contentType = response.headers()['content-type'] ?? '';
        let body: unknown = null;
        let parseError: string | undefined;
        if (rawBody.length > 0) {
          if (contentType.includes('json')) {
            try { body = addLegacyAcademicNames(JSON.parse(rawBody.toString('utf8'))); }
            catch (error) {
              body = rawBody.toString('utf8');
              parseError = error instanceof Error ? error.message : String(error);
            }
          } else {
            body = rawBody.toString('utf8');
          }
        }

        await attachJson('request-summary.json', {
          method: options.method,
          url: `${url.pathname}${url.search}`,
          timestamp,
          durationMs
        });
        await attachJson('response-summary.json', {
          status: response.status(),
          statusText: response.statusText(),
          durationMs,
          url: response.url(),
          ...(parseError ? { parseError } : {})
        });
        await attachJson('response-headers.json', redactHeaders(response.headers()));
        await attachJson('response-body.json', redactSensitiveData(body));
        return { response, body: body as T, rawBody, durationMs };
      } catch (error) {
        const durationMs = Date.now() - startedAt;
        await attachJson('request-summary.json', {
          method: options.method,
          url: `${url.pathname}${url.search}`,
          timestamp,
          durationMs
        });
        await attachJson('response-summary.json', {
          durationMs,
          networkError: error instanceof Error ? error.message : String(error)
        });
        throw error;
      }
    });
  }
}
