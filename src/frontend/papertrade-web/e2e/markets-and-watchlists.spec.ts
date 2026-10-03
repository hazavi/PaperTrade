import { expect, test } from '@playwright/test'

test('user can search markets and track a symbol', async ({ page }) => {
  await page.route('**/hubs/market/negotiate?**', (route) => route.abort())

  await page.route('**/api/markets/search?**', async (route) => {
    await route.fulfill({
      contentType: 'application/json',
      body: JSON.stringify([
        { symbol: 'AAPL', name: 'Apple Inc', exchange: 'NASDAQ', type: 'Stock', currency: 'USD' },
      ]),
    })
  })
  await page.route('**/api/markets/AAPL/quote', async (route) => {
    await route.fulfill({
      contentType: 'application/json',
      body: JSON.stringify({
        symbol: 'AAPL', currentPrice: 205.5, change: 2.5,
        percentChange: 1.23, open: 203, high: 207, low: 202,
        previousClose: 203, timestamp: new Date().toISOString(),
      }),
    })
  })
  await page.route('**/api/markets/AAPL/history?**', async (route) => {
    await route.fulfill({
      contentType: 'application/json',
      body: JSON.stringify([
        { time: '2026-09-29T14:30:00Z', open: 200, high: 203, low: 199, close: 202, volume: 1000 },
        { time: '2026-09-30T14:30:00Z', open: 202, high: 206, low: 201, close: 205.5, volume: 1200 },
      ]),
    })
  })

  const email = `week-two-${crypto.randomUUID()}@example.test`
  await page.goto('/register')
  await page.getByLabel('Display name').fill('Week Two Trader')
  await page.getByLabel('Email').fill(email)
  await page.getByLabel('Password').fill('a-long-passphrase')
  await page.getByRole('button', { name: 'Create account' }).click()

  await page.getByRole('link', { name: 'Markets' }).click()
  await page.getByLabel('Find a stock').fill('app')
  await expect(page.getByText('Apple Inc')).toBeVisible()
  await page.getByRole('link', { name: /AAPL/ }).click()
  await expect(page.getByRole('heading', { name: 'AAPL' })).toBeVisible()
  await expect(page.getByText('$205.50')).toBeVisible()

  await page.getByRole('link', { name: 'Add to watchlist' }).click()
  await page.getByLabel('Watchlist name').fill('Technology')
  await page.getByRole('button', { name: 'Create' }).click()
  await expect(page.getByRole('heading', { name: 'Technology' })).toBeVisible()
  await page.getByRole('button', { name: 'Add' }).click()
  await expect(page.getByRole('link', { name: 'AAPL' })).toBeVisible()
  await expect(page.getByText('$205.50')).toBeVisible()
})
