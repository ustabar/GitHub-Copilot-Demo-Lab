import { defineConfig } from '@playwright/test';
import { mkdirSync } from 'node:fs';
import { randomUUID } from 'node:crypto';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const directory = dirname(fileURLToPath(import.meta.url));
const root = resolve(directory, '..', '..');
const externalUrl = process.env.E2E_EXTERNAL_URL;
const runtime = resolve(root, '.runtime');
if (!externalUrl) mkdirSync(runtime, { recursive: true });
const database = resolve(runtime, `e2e-${randomUUID()}.db`);

export default defineConfig({
  testDir: './tests',
  fullyParallel: false,
  workers: 1,
  retries: 0,
  timeout: 30_000,
  expect: { timeout: 10_000 },
  reporter: 'list',
  use: {
    baseURL: externalUrl ?? 'http://127.0.0.1:5177',
    browserName: 'chromium',
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
  },
  // Each run owns both processes. Never reuse a developer's server or database.
  webServer: externalUrl ? undefined : [
    {
      command: 'dotnet run --project ..\\Orders.Api\\Orders.Api.csproj --no-launch-profile',
      cwd: directory,
      url: 'http://127.0.0.1:5107/health',
      timeout: 120_000,
      reuseExistingServer: false,
      env: {
        ASPNETCORE_URLS: 'http://127.0.0.1:5107',
        ASPNETCORE_ENVIRONMENT: 'Development',
        Orders__Mode: 'fixed',
        Orders__DatabasePath: database,
      },
    },
    {
      command: 'npm run dev -- --port 5177',
      cwd: directory,
      url: 'http://127.0.0.1:5177',
      timeout: 60_000,
      reuseExistingServer: false,
      env: { API_PROXY_TARGET: 'http://127.0.0.1:5107' },
    },
  ],
});
