import { expect, test } from '@playwright/test'

test('user reviews a market order and sees the filled position', async ({ page }) => {
  await page.route('**/hubs/market/negotiate?**', (route) => route.abort())

  let filled = false
  const quote = {
    symbol: 'AAPL', currentPrice: 100, change: 1, percentChange: 1,
    open: 99, high: 101, low: 98, previousClose: 99,
    timestamp: new Date().toISOString(),
  }

  await page.route('**/api/markets/AAPL/quote', (route) =>
    route.fulfill({ contentType: 'application/json', body: JSON.stringify(quote) }))
  await page.route('**/api/markets/AAPL/history?**', (route) =>
    route.fulfill({ contentType: 'application/json', body: JSON.stringify([
      { time: '2026-10-01T14:30:00Z', open: 98, high: 100, low: 97, close: 99, volume: 1000 },
      { time: '2026-10-02T14:30:00Z', open: 99, high: 101, low: 98, close: 100, volume: 1200 },
    ]) }))
  await page.route('**/api/portfolio', (route) =>
    route.fulfill({
      contentType: 'application/json',
      body: JSON.stringify({
        id: 'portfolio', name: 'Paper Portfolio',
        cashBalance: filled ? 99000 : 100000,
        initialBalance: 100000, marketValue: filled ? 1000 : 0,
        portfolioValue: 100000, unrealizedPnl: 0, realizedPnl: 0,
        totalReturnPercentage: 0,
        positions: filled ? [{
          id: 'position', symbol: 'AAPL', quantity: 10,
          averageEntryPrice: 100, currentPrice: 100, marketValue: 1000,
          unrealizedPnl: 0, returnPercentage: 0,
          updatedAt: new Date().toISOString(),
        }] : [],
      }),
    }))
  await page.route('**/api/orders', async (route) => {
    if (route.request().method() === 'POST') {
      filled = true
      await route.fulfill({
        status: 201,
        contentType: 'application/json',
        body: JSON.stringify({
          status: 'Filled', cashBalance: 99000, ownedQuantity: 10,
          order: {
            id: 'order', portfolioId: 'portfolio', symbol: 'AAPL',
            side: 'buy', type: 'market', quantity: 10,
            requestedPrice: 100, executedPrice: 100, totalValue: 1000,
            status: 'filled', createdAt: new Date().toISOString(),
            executedAt: new Date().toISOString(),
          },
        }),
      })
    } else {
      await route.fulfill({ contentType: 'application/json', body: '[]' })
    }
  })

  await page.goto('/register')
  await page.getByLabel('Display name').fill('Week Three Trader')
  await page.getByLabel('Email').fill(`week-three-${crypto.randomUUID()}@example.test`)
  await page.getByLabel('Password').fill('a-long-passphrase')
  await page.getByRole('button', { name: 'Create account' }).click()
  await expect(page).toHaveURL(/\/dashboard$/)

  await page.goto('/markets/AAPL')
  await page.getByRole('button', { name: 'Buy' }).click()
  await page.getByLabel('Quantity').fill('10')
  await expect(page.getByText('Estimated value: $1,000.00')).toBeVisible()
  await page.getByRole('button', { name: 'Review order' }).click()
  await expect(page.getByRole('heading', { name: 'Confirm buy order' })).toBeVisible()
  await page.getByRole('button', { name: 'Confirm' }).click()
  await expect(page.getByRole('heading', { name: 'Order filled' })).toBeVisible()
  await page.getByRole('button', { name: 'Done' }).click()

  await page.getByRole('link', { name: 'Portfolio' }).click()
  await expect(page.getByRole('link', { name: 'AAPL' })).toBeVisible()
  await expect(page.getByText('$99,000.00')).toBeVisible()
})
