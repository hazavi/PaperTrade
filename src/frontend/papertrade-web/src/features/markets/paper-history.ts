import type { HistoricalPrice, MarketQuote, Timeframe } from './market-types'

const timeframeSettings: Record<Timeframe, { count: number; stepMinutes: number; volatility: number }> = {
  '1D': { count: 78, stepMinutes: 5, volatility: 0.0018 },
  '1W': { count: 91, stepMinutes: 30, volatility: 0.0027 },
  '1M': { count: 120, stepMinutes: 120, volatility: 0.0038 },
  '3M': { count: 90, stepMinutes: 24 * 60, volatility: 0.009 },
  '1Y': { count: 252, stepMinutes: 24 * 60, volatility: 0.015 },
}

export function createPaperHistory(symbol: string, timeframe: Timeframe, quote: MarketQuote): HistoricalPrice[] {
  const settings = timeframeSettings[timeframe]
  const random = seededRandom(`${symbol}:${timeframe}:${quote.previousClose}`)
  const endTime = new Date(quote.timestamp).getTime()
  const safeEndTime = Number.isFinite(endTime) ? endTime : Date.now()
  const closes = [Math.max(quote.previousClose, 0.01)]

  for (let index = 1; index < settings.count; index += 1) {
    const cycle = Math.sin(index / 9) * settings.volatility * 0.35
    const change = (random() - 0.48) * settings.volatility + cycle
    closes.push(Math.max(0.01, closes[index - 1] * (1 + change)))
  }

  const scale = quote.currentPrice / closes[closes.length - 1]
  const scaledCloses = closes.map((close) => close * scale)

  return scaledCloses.map((close, index) => {
    const open = index === 0 ? close * (1 + (random() - 0.5) * settings.volatility) : scaledCloses[index - 1]
    const spread = Math.max(open, close) * settings.volatility * (0.45 + random())
    const time = new Date(safeEndTime - (settings.count - 1 - index) * settings.stepMinutes * 60_000)

    return {
      time: time.toISOString(),
      open: round(open),
      high: round(Math.max(open, close) + spread),
      low: round(Math.max(0.01, Math.min(open, close) - spread)),
      close: round(index === settings.count - 1 ? quote.currentPrice : close),
      volume: Math.round(80_000 + random() * 1_200_000),
      isSimulated: true,
      source: 'PaperTrade demo',
    }
  })
}

export function mergeLiveQuote(
  prices: HistoricalPrice[],
  quote: MarketQuote,
  timeframe: Timeframe,
): HistoricalPrice[] {
  if (!prices.length) return prices

  const intervalMinutes: Record<Timeframe, number> = {
    '1D': 5,
    '1W': 30,
    '1M': 60,
    '3M': 24 * 60,
    '1Y': 24 * 60,
  }
  const interval = intervalMinutes[timeframe] * 60_000
  const quoteTime = new Date(quote.timestamp).getTime()
  if (!Number.isFinite(quoteTime)) return prices
  const bucketTime = Math.floor(quoteTime / interval) * interval
  const last = prices.at(-1)!
  const lastTime = new Date(last.time).getTime()

  if (Math.floor(lastTime / interval) * interval === bucketTime) {
    return [
      ...prices.slice(0, -1),
      {
        ...last,
        high: Math.max(last.high, quote.currentPrice),
        low: Math.min(last.low, quote.currentPrice),
        close: quote.currentPrice,
      },
    ]
  }

  if (bucketTime > lastTime) {
    return [
      ...prices,
      {
        time: new Date(bucketTime).toISOString(),
        open: last.close,
        high: Math.max(last.close, quote.currentPrice),
        low: Math.min(last.close, quote.currentPrice),
        close: quote.currentPrice,
        volume: 0,
      },
    ]
  }

  return prices
}

function seededRandom(seedText: string) {
  let seed = 2166136261
  for (let index = 0; index < seedText.length; index += 1) {
    seed ^= seedText.charCodeAt(index)
    seed = Math.imul(seed, 16777619)
  }

  return () => {
    seed += 0x6d2b79f5
    let value = seed
    value = Math.imul(value ^ (value >>> 15), value | 1)
    value ^= value + Math.imul(value ^ (value >>> 7), value | 61)
    return ((value ^ (value >>> 14)) >>> 0) / 4294967296
  }
}

function round(value: number) {
  return Math.round(value * 10000) / 10000
}
