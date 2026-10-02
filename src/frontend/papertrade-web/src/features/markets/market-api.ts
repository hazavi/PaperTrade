import { apiRequest } from '../../lib/api-client'
import type {
  AssetSummary,
  HistoricalPrice,
  MarketQuote,
  Timeframe,
} from './market-types'

export function searchMarkets(query: string) {
  return apiRequest<AssetSummary[]>(
    `/api/markets/search?q=${encodeURIComponent(query)}`,
  )
}

export function getMarketQuote(symbol: string) {
  return apiRequest<MarketQuote>(
    `/api/markets/${encodeURIComponent(symbol)}/quote`,
  )
}

export function getMarketHistory(
  symbol: string,
  timeframe: Timeframe,
) {
  return apiRequest<HistoricalPrice[]>(
    `/api/markets/${encodeURIComponent(symbol)}/history?timeframe=${timeframe}`,
  )
}
