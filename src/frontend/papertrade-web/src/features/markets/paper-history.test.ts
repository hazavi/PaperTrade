import { describe, expect, it } from 'vitest'
import { createPaperHistory, mergeLiveQuote } from './paper-history'
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

  it('merges a realtime quote into the current candle', () => {
    const prices = createPaperHistory('MSFT', '1D', quote)
    const liveQuote = { ...quote, currentPrice: 530, timestamp: quote.timestamp }

    const merged = mergeLiveQuote(prices, liveQuote, '1D')

    expect(merged).toHaveLength(prices.length)
    expect(merged.at(-1)?.close).toBe(530)
    expect(merged.at(-1)?.high).toBeGreaterThanOrEqual(530)
  })
})
