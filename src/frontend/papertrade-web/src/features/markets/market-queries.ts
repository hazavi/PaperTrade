import { useQuery } from '@tanstack/react-query'
import {
  getMarketHistory,
  getMarketQuote,
  getInstrument,
  searchMarkets,
} from './market-api'
import type { Timeframe } from './market-types'

export const marketKeys = {
  search: (query: string) => ['markets', 'search', query] as const,
  quote: (symbol: string) => ['markets', 'quote', symbol] as const,
  instrument: (symbol: string) => ['markets', 'instrument', symbol] as const,
  history: (symbol: string, timeframe: Timeframe) =>
    ['markets', 'history', symbol, timeframe] as const,
}

export function useMarketSearch(query: string) {
  return useQuery({
    queryKey: marketKeys.search(query),
    queryFn: () => searchMarkets(query),
    enabled: query.trim().length >= 2,
    staleTime: 10 * 60_000,
    retry: 1,
  })
}

export function useMarketQuote(symbol: string) {
  return useQuery({
    queryKey: marketKeys.quote(symbol),
    queryFn: () => getMarketQuote(symbol),
    enabled: Boolean(symbol),
    staleTime: 15_000,
    retry: 1,
  })
}

export function useInstrument(symbol: string) {
  return useQuery({
    queryKey: marketKeys.instrument(symbol),
    queryFn: () => getInstrument(symbol),
    enabled: Boolean(symbol),
    staleTime: 60 * 60_000,
  })
}

export function useMarketHistory(
  symbol: string,
  timeframe: Timeframe,
) {
  return useQuery({
    queryKey: marketKeys.history(symbol, timeframe),
    queryFn: () => getMarketHistory(symbol, timeframe),
    enabled: Boolean(symbol),
    staleTime: 60 * 60_000,
    retry: 1,
  })
}
