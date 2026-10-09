import { afterEach, describe, expect, it, vi } from 'vitest'

async function request() {
  vi.stubEnv('VITE_API_BASE_URL', 'http://localhost:5044')
  const { apiRequest } = await import('./api-client')
  return apiRequest('/api/orders', { method: 'POST', body: '{}' })
}

afterEach(() => { vi.unstubAllGlobals(); vi.unstubAllEnvs() })

describe('API rejection messages', () => {
  it('shows the reason from ASP.NET problem+json instead of a generic 409', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify({
      title: 'The quote is too old for an order. Refresh market data and try again.',
      status: 409,
    }), { status: 409, headers: { 'Content-Type': 'application/problem+json; charset=utf-8' } })))
    await expect(request()).rejects.toMatchObject({ status: 409, message: 'The quote is too old for an order. Refresh market data and try again.' })
  })

  it('preserves field errors and uses a specific detail when supplied', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify({
      title: 'Validation failed.', detail: 'Quantity must match the instrument step.', errors: { quantity: ['Invalid step.'] },
    }), { status: 400, headers: { 'Content-Type': 'Application/Problem+Json' } })))
    await expect(request()).rejects.toMatchObject({ status: 400, message: 'Quantity must match the instrument step.', errors: { quantity: ['Invalid step.'] } })
  })

  it('handles JSON conflict errors without hiding the reason', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify({ error: 'Insufficient available cash.' }), { status: 409, headers: { 'Content-Type': 'application/json' } })))
    await expect(request()).rejects.toMatchObject({ message: 'Insufficient available cash.' })
  })

  it('falls back safely for empty or non-JSON responses', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response('Proxy failed', { status: 503, headers: { 'Content-Type': 'text/html' } })))
    await expect(request()).rejects.toMatchObject({ status: 503, message: 'Request failed with status 503.' })
  })
})
