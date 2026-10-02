import { expect, test } from '@playwright/test'

test('user can register, log out, and log back in', async ({
  page,
}) => {
  const email = `week-one-${crypto.randomUUID()}@example.test`
  const password = 'a-long-passphrase'
  const displayName = 'Week One Trader'

  await page.goto('/register')

  await page.getByLabel('Display name').fill(displayName)
  await page.getByLabel('Email').fill(email)
  await page.getByLabel('Password').fill(password)
  await page.getByRole('button', {
    name: 'Create account',
  }).click()

  await expect(page).toHaveURL(/\/dashboard$/)
  await expect(
    page.getByRole('heading', {
      name: `Welcome, ${displayName}`,
    }),
  ).toBeVisible()
  await expect(page.getByText('$100,000.00')).toHaveCount(2)

  await page.getByRole('button', { name: 'Log out' }).click()

  await expect(page).toHaveURL(/\/login$/)

  await page.getByLabel('Email').fill(email)
  await page.getByLabel('Password').fill(password)
  await page.getByRole('button', { name: 'Log in' }).click()

  await expect(page).toHaveURL(/\/dashboard$/)
  await expect(
    page.getByRole('heading', {
      name: `Welcome, ${displayName}`,
    }),
  ).toBeVisible()
})
