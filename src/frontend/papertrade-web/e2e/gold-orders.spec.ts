import { expect, test } from '@playwright/test'

test('fractional gold orders show a rejection reason and can buy then sell', async ({ page }) => {
  let owned = 0
  let attempts = 0
  await page.route('**/hubs/**', route => route.abort())
  await page.route('**/api/**', route => route.fulfill({ json: {} }))
  await page.route('**/api/auth/me', route => route.fulfill({ json: { id: 'user', email: 'gold@example.test', displayName: 'Gold Trader' } }))
  await page.route('**/api/chart-layouts', route => route.fulfill({ json: [] }))
  await page.route('**/api/markets/pair/XAU/USD/instrument', route => route.fulfill({ json: { id: 'gold', symbol: 'XAU/USD', displayName: 'Gold / US Dollar', assetClass: 'metal', exchange: 'FOREX', quoteCurrency: 'USD', baseCurrency: 'XAU', pricePrecision: 3, quantityPrecision: 2, tickSize: 0.001, minimumOrderSize: 0.01, lotSize: 100, isTradable: true } }))
  await page.route('**/api/markets/pair/XAU/USD/quote', route => route.fulfill({ json: { symbol: 'XAU/USD', currentPrice: 4139.887, ask: 4139.888, bid: 4139.886, open: 4130, high: 4140, low: 4120, previousClose: 4130, change: 9.887, percentChange: 0.24, timestamp: new Date().toISOString(), source: 'Twelve Data' } }))
  await page.route('**/api/markets/pair/XAU/USD/history?**', route => route.fulfill({ json: [{ time: new Date().toISOString(), open: 4130, high: 4140, low: 4120, close: 4139.887, volume: 0, source: 'Twelve Data' }] }))
  await page.route('**/api/portfolio', route => route.fulfill({ json: { id: 'portfolio', cashBalance: 100000, positions: owned ? [{ id: 'position', instrumentId: 'gold', symbol: 'XAU/USD', quantity: owned }] : [] } }))
  await page.route('**/api/orders', async route => {
    if (route.request().method() !== 'POST') { await route.fulfill({ json: [] }); return }
    const order = route.request().postDataJSON()
    expect(order.symbol).toBe('XAU/USD')
    expect(order.quantity).toBe(0.03)
    attempts += 1
    if (attempts === 1) {
      await route.fulfill({ status: 409, contentType: 'application/problem+json', body: JSON.stringify({ title: 'The quote is too old for an order. Refresh market data and try again.', status: 409 }) })
      return
    }
    owned = order.side === 'buy' ? owned + order.quantity : owned - order.quantity
    await route.fulfill({ status: 201, json: { status: 'Filled', cashBalance: 100000, ownedQuantity: owned, order: { ...order, id: `order-${attempts}`, status: 'filled', executedPrice: 4139.887 } } })
  })
  await page.goto('/markets/pair/XAU/USD')
  await page.getByLabel('Quantity', { exact: true }).fill('0.03')
  await expect(page.getByRole('button', { name: 'Review order', exact: true })).toBeEnabled()
  await page.getByRole('button', { name: 'Review order', exact: true }).click()
  await page.getByRole('button', { name: 'Confirm', exact: true }).click()
  await expect(page.getByRole('alert')).toContainText('The quote is too old')
  await expect(page.getByRole('alert')).not.toContainText('Request failed with status 409')
  await page.getByRole('button', { name: 'Confirm', exact: true }).click()
  await expect(page.getByRole('heading', { name: 'Order filled' })).toBeVisible()
  await page.getByRole('button', { name: 'Done', exact: true }).click()
  await page.getByRole('button', { name: /^Sell/ }).click()
  await expect(page.getByText('Sell closes an owned position.', { exact: false })).toContainText('0.03')
  await page.getByLabel('Quantity', { exact: true }).fill('0.03')
  await page.getByRole('button', { name: 'Review order', exact: true }).click()
  await page.getByRole('button', { name: 'Confirm', exact: true }).click()
  await expect(page.getByRole('heading', { name: 'Order filled' })).toBeVisible()
  expect(owned).toBe(0)
  expect(attempts).toBe(3)
})
