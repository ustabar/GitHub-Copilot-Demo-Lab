import { test as base, expect, type Page } from '@playwright/test';
import { randomUUID } from 'node:crypto';

const test = base.extend<{ isolatedCustomer: void }>({
  isolatedCustomer: [async ({ page }, use) => {
    const suffix = randomUUID();
    const errors: string[] = [];
    page.on('pageerror', error => errors.push(error.message));
    page.on('console', message => {
      // Chromium emits resource errors for the intentionally rejected 409 and dropped response.
      if (message.type() === 'error' && !/Failed to load resource:.*(?:409|net::ERR_FAILED)/.test(message.text())) {
        errors.push(message.text());
      }
    });
    await page.route('**/api/orders', async route => {
      const headers = route.request().headers();
      await route.continue({ headers: { ...headers, 'x-customer-id': `${headers['x-customer-id']}-${suffix}` } });
    });
    await use();
    expect(errors, 'No unexpected browser or JavaScript errors').toEqual([]);
  }, { auto: true }],
});

async function openPortal(page: Page, query = '') {
  await page.goto(`/${query}`);
  await expect(page.getByTestId('server-mode')).toHaveText('fixed');
  await expect(page.getByRole('button', { name: 'Listeyi yenile' })).toBeEnabled();
  await expect(page.getByTestId('order-count')).toHaveText('0');
}

async function createOrder(page: Page) {
  await page.getByRole('button', { name: 'Sipariş gönder', exact: true }).click();
  await expect(page.getByTestId('request-result').first()).toContainText('HTTP 201');
  await expect(page.getByRole('button', { name: 'Aynı anahtarla tekrar dene', exact: true })).toBeEnabled();
  return (await page.getByTestId('result-order-id').first().textContent())!;
}

test('retry keeps one order; new intent creates a different order and key', async ({ page }) => {
  await openPortal(page);
  const key = await page.getByTestId('current-key').textContent();
  const id = await createOrder(page);
  await page.getByRole('button', { name: 'Aynı anahtarla tekrar dene', exact: true }).click();
  await expect(page.getByTestId('request-result').first()).toContainText('HTTP 200');
  await expect(page.getByTestId('result-order-id').first()).toHaveText(id);
  await expect(page.getByTestId('current-key')).toHaveText(key!);
  await expect(page.getByTestId('order-count')).toHaveText('1');
  await page.getByRole('button', { name: 'Yeni sipariş', exact: true }).click();
  await expect(page.getByTestId('current-key')).not.toHaveText(key!);
  const secondId = await createOrder(page);
  expect(secondId).not.toBe(id);
  await expect(page.getByTestId('order-count')).toHaveText('2');
});

test('draft changes cannot silently change retry payload; deliberate conflict returns 409', async ({ page }) => {
  await openPortal(page);
  const id = await createOrder(page);
  await page.getByLabel('Adet', { exact: true }).fill('3');
  await expect(page.getByText('Taslak değişti.', { exact: false })).toBeVisible();
  await expect(page.getByRole('button', { name: 'İstek kaydedildi', exact: true })).toBeDisabled();
  await page.getByRole('button', { name: 'Aynı anahtarla tekrar dene', exact: true }).click();
  await expect(page.getByTestId('request-result').first()).toContainText('HTTP 200');
  await expect(page.getByTestId('result-order-id').first()).toHaveText(id);
  await page.getByText('Çakışma deneyini aç', { exact: true }).click();
  await page.getByRole('button', { name: 'Aynı anahtar + farklı adet → 409', exact: true }).click();
  await expect(page.getByTestId('request-result').first()).toContainText('HTTP 409');
  await expect(page.getByTestId('request-result').first()).toContainText('idempotency_conflict');
  await expect(page.getByTestId('order-count')).toHaveText('1');
  await expect(page.locator('tbody tr').first().locator('td').nth(2)).toHaveText('1');
});

test('switching tenants clears previous intent and scopes the order list', async ({ page }) => {
  await openPortal(page);
  const alphaId = await createOrder(page);
  const alphaKey = await page.getByTestId('current-key').textContent();
  await page.getByLabel('Müşteri hesabı').selectOption('beta');
  await expect(page.getByTestId('current-key')).not.toHaveText(alphaKey!);
  await expect(page.getByTestId('request-result')).toHaveCount(0);
  await expect(page.getByRole('button', { name: 'Aynı anahtarla tekrar dene', exact: true })).toBeDisabled();
  await expect(page.getByRole('button', { name: 'Listeyi yenile' })).toBeEnabled();
  await expect(page.getByTestId('order-count')).toHaveText('0');
  await expect(page.getByText(alphaId, { exact: true })).toHaveCount(0);
  const betaId = await createOrder(page);
  expect(betaId).not.toBe(alphaId);
  await page.getByLabel('Müşteri hesabı').selectOption('alpha');
  await expect(page.getByRole('button', { name: 'Listeyi yenile' })).toBeEnabled();
  await expect(page.getByTestId('order-count')).toHaveText('1');
  await expect(page.locator('tbody')).toContainText(alphaId);
  await expect(page.getByText(betaId, { exact: true })).toHaveCount(0);
});

test('lost response after server commit retains key and original payload for safe retry', async ({ page }) => {
  await openPortal(page);
  const key = await page.getByTestId('current-key').textContent();
  let dropped = false;
  let originalCustomer = '';
  // route.fetch bypasses other route handlers, so explicitly carry the unique test scope.
  const networkCustomer = `alpha-network-${randomUUID()}`;
  await page.route('**/api/orders', async route => {
    const headers = { ...route.request().headers(), 'x-customer-id': networkCustomer };
    if (route.request().method() === 'POST' && !dropped) {
      originalCustomer = headers['x-customer-id'];
      const response = await route.fetch({ headers });
      expect(response.status()).toBe(201);
      dropped = true;
      await route.abort('failed');
    } else {
      await route.continue({ headers });
    }
  });
  await page.getByRole('button', { name: 'Sipariş gönder', exact: true }).click();
  await expect(page.getByTestId('request-result').first()).toContainText('Ağ hatası');
  await expect(page.getByTestId('current-key')).toHaveText(key!);
  await expect(page.getByTestId('order-count')).toHaveText('1');
  await page.getByLabel('Adet', { exact: true }).fill('9');
  await page.getByRole('button', { name: 'Aynı anahtarla tekrar dene', exact: true }).click();
  await expect(page.getByTestId('request-result').first()).toContainText('HTTP 200');
  await expect(page.getByTestId('order-count')).toHaveText('1');
  await expect(page.locator('tbody tr').first().locator('td').nth(2)).toHaveText('1');
  expect(originalCustomer).toBe(networkCustomer);
});

test('synchronous duplicate submissions send only one create request', async ({ page }) => {
  await openPortal(page);
  let posts = 0;
  page.on('request', request => {
    if (request.method() === 'POST' && request.url().endsWith('/api/orders')) posts++;
  });
  await page.locator('form').evaluate(form => {
    form.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
    form.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
  });
  await expect(page.getByTestId('request-result').first()).toContainText('HTTP 201');
  await expect(page.getByRole('button', { name: 'Aynı anahtarla tekrar dene', exact: true })).toBeEnabled();
  expect(posts).toBe(1);
  await expect(page.getByTestId('order-count')).toHaveText('1');
});

test('explicit theme takes precedence over system preference and mobile layout fits', async ({ page }) => {
  await page.emulateMedia({ colorScheme: 'dark' });
  await page.setViewportSize({ width: 390, height: 844 });
  await openPortal(page, '?scoutTheme=light');
  await expect(page.locator('html')).toHaveAttribute('data-theme', 'light');
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true);
  await page.goto('/?scoutTheme=dark');
  await expect(page.locator('html')).toHaveAttribute('data-theme', 'dark');
  await page.goto('/');
  await expect(page.locator('html')).toHaveAttribute('data-theme', 'dark');
});

for (const body of ['not-json', '{"id":"incomplete"}']) {
  test(`malformed successful response is not presented as success: ${body}`, async ({ page }) => {
    await openPortal(page);
    const key = await page.getByTestId('current-key').textContent();
    await page.route('**/api/orders', async route => {
      if (route.request().method() === 'POST') {
        await route.fulfill({ status: 201, contentType: 'application/json', body });
      } else {
        await route.fallback();
      }
    });
    await page.getByRole('button', { name: 'Sipariş gönder', exact: true }).click();
    const result = page.getByTestId('request-result').first();
    await expect(result).toContainText('Sunucu yanıtı doğrulanamadı');
    await expect(result).toContainText('HTTP 201');
    await expect(result).toHaveClass(/result-error/);
    await expect(page.getByTestId('result-order-id')).toHaveCount(0);
    await expect(page.getByTestId('current-key')).toHaveText(key!);
    await expect(page.getByRole('button', { name: 'Aynı anahtarla tekrar dene', exact: true })).toBeEnabled();
  });
}

test('malformed order list is surfaced rather than rendered as valid orders', async ({ page }) => {
  await openPortal(page);
  await page.route('**/api/orders', async route => {
    if (route.request().method() === 'GET') {
      await route.fulfill({ json: [{ id: 'incomplete' }] });
    } else {
      await route.fallback();
    }
  });
  await page.getByRole('button', { name: 'Listeyi yenile', exact: true }).click();
  await expect(page.getByRole('alert')).toContainText('Sunucu geçerli bir sipariş listesi döndürmedi.');
  await expect(page.locator('tbody tr')).toHaveCount(0);
});
