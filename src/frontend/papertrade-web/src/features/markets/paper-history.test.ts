import { describe, expect, it } from 'vitest'
import { createPaperHistory } from './paper-history'
import type { MarketQuote } from './market-types'

const quote: MarketQuote = {
  symbol: 'MSFT',
  currentPrice: 526.04,
  change: 8.51,
  percentChange: 1.64,
  open: 518.84,
  high: 532.35,
  low: 518.84,
  previousClose: 517.53,
  timestamp: '2026-10-05T16:00:00Z',
}

describe('createPaperHistory', () => {
  it('creates deterministic OHLC candles anchored to the current quote', () => {
    const first = createPaperHistory('MSFT', '1W', quote)
    const second = createPaperHistory('MSFT', '1W', quote)

    expect(first).toEqual(second)
    expect(first).toHaveLength(91)
    expect(first.at(-1)?.close).toBe(quote.currentPrice)
    expect(first.every((bar) => bar.high >= Math.max(bar.open, bar.close))).toBe(true)
    expect(first.every((bar) => bar.low <= Math.min(bar.open, bar.close))).toBe(true)
  })
})
