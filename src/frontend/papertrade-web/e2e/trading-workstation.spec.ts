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
  await expect(page.getByLabel('Favorite drawing tools')).toHaveCount(0)
  await page.getByRole('button', { name: 'Show Line tools' }).click()
  await page.getByRole('button', { name: 'Add Trend line to favorites' }).click()
  await page.getByRole('button', { name: 'Close Line tools' }).click()
  const favoriteToolbar = page.getByLabel('Favorite drawing tools')
  await expect(favoriteToolbar).toBeVisible()
  await expect(favoriteToolbar.getByRole('button', { name: 'Trend line' })).toBeVisible()
  const dragHandle = page.getByRole('button', { name: 'Drag favorite tools' })
  const originalPosition = await favoriteToolbar.boundingBox()
  const handlePosition = await dragHandle.boundingBox()
  if (!originalPosition || !handlePosition) throw new Error('Favorite toolbar was not measurable.')
  await page.mouse.move(handlePosition.x + handlePosition.width / 2, handlePosition.y + handlePosition.height / 2)
  await page.mouse.down()
  await page.mouse.move(handlePosition.x + 130, handlePosition.y + 90, { steps: 6 })
  await page.mouse.up()
  const movedPosition = await favoriteToolbar.boundingBox()
  expect(movedPosition?.x).toBeGreaterThan(originalPosition.x + 40)
  expect(movedPosition?.y).toBeGreaterThan(originalPosition.y + 20)
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

  await page.getByRole('button', { name: 'Show Line tools' }).click()
  await page.getByRole('menuitem', { name: 'Horizontal line' }).click()
  await expect(page.getByText('Click the chart to start a horizontal line.')).toBeVisible()
  const drawingLayer = page.locator('.chart-drawing-overlay')
  await drawingLayer.click({ position: { x: 300, y: 250 } })
  await expect(page.getByRole('button', { name: 'Clear drawings (1)' })).toBeEnabled()

  await favoriteToolbar.getByRole('button', { name: 'Trend line' }).click()
  await drawingLayer.click({ position: { x: 350, y: 300 } })
  await drawingLayer.click({ position: { x: 520, y: 220 } })
  await expect(drawingLayer.locator('line')).toHaveCount(2)
  await expect(page.getByRole('button', { name: 'Clear drawings (2)' })).toBeEnabled()

  await page.getByRole('button', { name: 'Show Channel and Fibonacci tools' }).click()
  await page.getByRole('menuitem', { name: 'Fibonacci retracement' }).click()
  await drawingLayer.click({ position: { x: 420, y: 180 } })
  await drawingLayer.click({ position: { x: 620, y: 360 } })
  await expect(drawingLayer.locator('.fibonacci-drawing line')).toHaveCount(7)
  await expect(page.getByRole('button', { name: 'Clear drawings (3)' })).toBeEnabled()

  await page.getByRole('button', { name: 'Show Line tools' }).click()
  await page.getByRole('menuitem', { name: 'Arrow line' }).click()
  await drawingLayer.click({ position: { x: 260, y: 350 } })
  await drawingLayer.click({ position: { x: 390, y: 265 } })
  await expect(drawingLayer.locator('.drawing-arrow')).toHaveCount(1)

  await page.getByRole('button', { name: 'Rectangle', exact: true }).click()
  await drawingLayer.click({ position: { x: 520, y: 160 } })
  await drawingLayer.click({ position: { x: 660, y: 250 } })
  const rectangle = drawingLayer.locator('.chart-drawing-object rect').last()
  const rectangleBeforeMove = await rectangle.boundingBox()
  if (!rectangleBeforeMove) throw new Error('Rectangle was not measurable.')
  await page.mouse.move(rectangleBeforeMove.x + rectangleBeforeMove.width / 2, rectangleBeforeMove.y + rectangleBeforeMove.height / 2)
  await page.mouse.down()
  await page.mouse.move(rectangleBeforeMove.x + rectangleBeforeMove.width / 2 + 55, rectangleBeforeMove.y + rectangleBeforeMove.height / 2 + 35, { steps: 5 })
  await page.mouse.up()
  const rectangleAfterMove = await rectangle.boundingBox()
  expect(rectangleAfterMove?.x).toBeGreaterThan(rectangleBeforeMove.x + 35)
  expect(rectangleAfterMove?.y).toBeGreaterThan(rectangleBeforeMove.y + 20)

  await page.getByRole('button', { name: 'Show Shape tools' }).click()
  await page.getByRole('menuitem', { name: 'Ellipse' }).click()
  await drawingLayer.click({ position: { x: 560, y: 180 } })
  await drawingLayer.click({ position: { x: 700, y: 280 } })
  await expect(drawingLayer.locator('ellipse')).toHaveCount(1)
  await expect(page.getByRole('button', { name: 'Clear drawings (6)' })).toBeEnabled()
  await page.screenshot({ path: 'test-results/trading-workstation.png', fullPage: true })

  await page.setViewportSize({ width: 390, height: 844 })
  await page.reload()
  await expect(page.getByLabel('Interactive historical price chart')).toBeVisible()
  await expect(page.getByLabel('Favorite drawing tools')).toBeVisible()
  await expect(page.getByLabel('Order ticket')).toBeVisible()
  await page.screenshot({ path: 'test-results/trading-workstation-mobile.png', fullPage: true })
})
