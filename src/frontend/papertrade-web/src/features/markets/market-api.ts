import { apiRequest } from '../../lib/api-client'
import type {
  AssetSummary,
  HistoricalPrice,
  MarketQuote,
  Instrument,
  Timeframe,
} from './market-types'

export function searchMarkets(query: string) {
  return apiRequest<AssetSummary[]>(
    `/api/markets/search?q=${encodeURIComponent(query)}`,
  )
}

export function getMarketQuote(symbol: string) {
  return apiRequest<MarketQuote>(
    `${marketPath(symbol)}/quote`,
  )
}

export function getInstrument(symbol: string) {
  return apiRequest<Instrument>(`${marketPath(symbol)}/instrument`)
}

export function getMarketHistory(
  symbol: string,
  timeframe: Timeframe,
) {
  return apiRequest<HistoricalPrice[]>(
    `${marketPath(symbol)}/history?timeframe=${timeframe}`,
  )
}

function marketPath(symbol: string) {
  const parts = symbol.split('/')
  return parts.length === 2
    ? `/api/markets/pair/${encodeURIComponent(parts[0])}/${encodeURIComponent(parts[1])}`
    : `/api/markets/${encodeURIComponent(symbol)}`
}
