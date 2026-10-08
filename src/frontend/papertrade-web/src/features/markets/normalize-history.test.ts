import { describe, expect, it } from 'vitest'
import { normalizeHistory } from './normalize-history'

const bar = { time: '2026-10-05T14:30:00Z', open: 100, high: 102, low: 99, close: 101, volume: 10, source: 'Twelve Data' }
describe('normalizeHistory', () => {
  it('sorts candles and uses the latest value for duplicate seconds', () => {
    const earlier = { ...bar, time: '2026-10-05T14:25:00Z' }
    const corrected = { ...bar, close: 102 }
    expect(normalizeHistory([bar, earlier, corrected])).toEqual([earlier, corrected])
  })
  it('rejects invalid OHLC, nonfinite prices and invalid timestamps', () => {
    expect(normalizeHistory([bar, { ...bar, high: 90 }, { ...bar, low: 110 }, { ...bar, time: 'invalid' }, { ...bar, close: NaN }, { ...bar, volume: -1 }])).toEqual([bar])
  })
})
