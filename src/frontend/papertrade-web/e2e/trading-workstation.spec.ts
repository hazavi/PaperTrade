import { expect, test } from '@playwright/test'

test('trader can use chart tools and plan take-profit and stop-loss levels', async ({ page }) => {
  const now = new Date().toISOString()
  await page.route('**/hubs/**', (route) => route.abort())
  await page.route('**/api/auth/me', (route) => route.fulfill({
    contentType: 'application/json',
    body: JSON.stringify({ id: 'user', email: 'trader@example.test', displayName: 'Chart Trader' }),
  }))
  await page.route('**/api/markets/AAPL/quote', (route) => route.fulfill({
    contentType: 'application/json',
    body: JSON.stringify({ symbol: 'AAPL', currentPrice: 100, change: 1.25, percentChange: 1.27, open: 99, high: 102, low: 98, previousClose: 98.75, timestamp: now }),
  }))
  await page.route('**/api/markets/AAPL/history?**', (route) => route.fulfill({
    contentType: 'application/json',
    body: '[]',
  }))
  await page.route('**/api/portfolio', (route) => route.fulfill({
    contentType: 'application/json',
    body: JSON.stringify({ id: 'portfolio', name: 'Paper Portfolio', cashBalance: 100000, initialBalance: 100000, marketValue: 0, portfolioValue: 100000, unrealizedPnl: 0, realizedPnl: 0, totalReturnPercentage: 0, positions: [] }),
  }))

  await page.goto('/markets/AAPL')

  await expect(page.getByText('History offline')).toBeVisible()
  await page.getByRole('button', { name: 'Use demo candles' }).click()
  await expect(page.getByText('Demo candles')).toBeVisible()
  await expect(page.getByLabel('Interactive historical price chart')).toBeVisible()
  await expect(page.getByRole('button', { name: 'Candles' })).toHaveAttribute('aria-pressed', 'true')
  await page.getByRole('button', { name: 'Indicators' }).click()
  await page.getByRole('button', { name: /SMA 20/ }).click()
  await page.getByRole('button', { name: /EMA 20/ }).click()
  await page.getByRole('button', { name: /Bollinger/ }).click()
  await expect(page.getByRole('button', { name: /SMA 20/ })).toHaveAttribute('aria-pressed', 'true')
  await page.getByRole('button', { name: 'Indicators' }).click()
  await page.getByLabel('Quantity').fill('10')
  await page.getByRole('switch', { name: 'Enable take profit' }).click()
  await page.getByRole('switch', { name: 'Enable stop loss' }).click()
  await page.getByRole('spinbutton', { name: 'Take profit' }).fill('103')
  await page.getByRole('spinbutton', { name: 'Stop loss' }).fill('98')
  await expect(page.getByText('Trade value')).toBeVisible()
  await expect(page.getByRole('button', { name: 'Review order' })).toBeEnabled()

  await page.getByRole('button', { name: 'Horizontal line' }).first().click()
  await expect(page.getByText('Click the chart to place a horizontal line.')).toBeVisible()
  await page.getByLabel('Interactive historical price chart').click({ position: { x: 300, y: 250 } })
  await expect(page.getByRole('button', { name: 'Clear drawings (1)' })).toBeEnabled()

  await page.getByRole('button', { name: 'Trend line' }).first().click()
  const drawingLayer = page.locator('.chart-drawing-overlay')
  await drawingLayer.click({ position: { x: 350, y: 300 } })
  await drawingLayer.click({ position: { x: 520, y: 220 } })
  await expect(drawingLayer.locator('line')).toHaveCount(1)
  await expect(page.getByRole('button', { name: 'Clear drawings (2)' })).toBeEnabled()

  await page.getByRole('button', { name: 'Fibonacci retracement' }).first().click()
  await drawingLayer.click({ position: { x: 420, y: 180 } })
  await drawingLayer.click({ position: { x: 620, y: 360 } })
  await expect(drawingLayer.locator('.fibonacci-drawing line')).toHaveCount(7)
  await expect(page.getByRole('button', { name: 'Clear drawings (3)' })).toBeEnabled()
  await page.screenshot({ path: 'test-results/trading-workstation.png', fullPage: true })

  await page.setViewportSize({ width: 390, height: 844 })
  await page.reload()
  await expect(page.getByLabel('Interactive historical price chart')).toBeVisible()
  await expect(page.getByLabel('Order ticket')).toBeVisible()
  await page.screenshot({ path: 'test-results/trading-workstation-mobile.png', fullPage: true })
})
