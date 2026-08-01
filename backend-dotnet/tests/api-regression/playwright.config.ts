import { defineConfig } from '@playwright/test';
import { env } from './src/config/environment';

export default defineConfig({
  testDir: './tests',
  outputDir: './test-results',
  timeout: 90_000,
  expect: { timeout: 10_000 },
  fullyParallel: false,
  workers: 1,
  retries: process.env.CI ? 1 : 0,
  globalSetup: require.resolve('./global-setup'),
  globalTeardown: require.resolve('./global-teardown'),
  reporter: [
    ['list'],
    ['html', { outputFolder: 'playwright-report', open: 'never' }],
    ['allure-playwright', { resultsDir: 'allure-results', detail: true }]
  ],
  use: {
    baseURL: env.apiBaseUrl,
    extraHTTPHeaders: { Accept: 'application/json' },
    // API traces may contain raw Authorization headers. Diagnostic evidence is attached redacted instead.
    trace: 'off'
  }
});
