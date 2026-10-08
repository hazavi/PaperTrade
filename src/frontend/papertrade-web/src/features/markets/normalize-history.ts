import type { HistoricalPrice } from './market-types'

// The chart requires unique, increasing timestamps and valid OHLC geometry.
// Reject invalid bars instead of silently altering their prices.
export function normalizeHistory(prices: HistoricalPrice[]): HistoricalPrice[] {
  const unique = new Map<number, HistoricalPrice>()
  for (const bar of prices) {
    const timestamp = Math.floor(Date.parse(bar.time) / 1000)
    if (!Number.isFinite(timestamp) || ![bar.open, bar.high, bar.low, bar.close, bar.volume].every(Number.isFinite)) continue
    if (bar.low <= 0 || bar.volume < 0 || bar.high < Math.max(bar.open, bar.close) || bar.low > Math.min(bar.open, bar.close)) continue
    unique.set(timestamp, bar)
  }
  return [...unique.entries()].sort(([a], [b]) => a - b).map(([, bar]) => bar)
}
