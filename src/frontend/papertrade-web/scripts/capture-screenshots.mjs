import { mkdir } from 'node:fs/promises'
import { fileURLToPath } from 'node:url'
import { chromium } from '@playwright/test'

const baseUrl = process.env.PAPERTRADE_WEB_URL ?? 'http://localhost:5173'
const outputDirectory = fileURLToPath(
  new URL('../../../../docs/screenshots/', import.meta.url),
)

await mkdir(outputDirectory, { recursive: true })

const browser = await chromium.launch()
try {
  const page = await browser.newPage({ viewport: { width: 1440, height: 1000 } })
  await page.goto(`${baseUrl}/register`)
  await page.getByLabel('Display name').fill('Documentation Trader')
  await page.getByLabel('Email').fill(`docs-${crypto.randomUUID()}@example.test`)
  await page.getByLabel('Password').fill('a-long-documentation-passphrase')
  await page.getByRole('button', { name: 'Create account' }).click()
  await page.waitForURL('**/dashboard')
  await page.getByText('$100,000.00').first().waitFor()
  await page.screenshot({ path: `${outputDirectory}/dashboard.png`, fullPage: true })

  await page.goto(`${baseUrl}/alerts`)
  await page.getByLabel('Symbol').fill('AAPL')
  await page.getByLabel('Target price').fill('500')
  await page.getByRole('button', { name: 'Create alert' }).click()
  await page.getByText('AAPL above $500.00').waitFor()
  await page.screenshot({ path: `${outputDirectory}/price-alerts.png`, fullPage: true })
} finally {
  await browser.close()
}
