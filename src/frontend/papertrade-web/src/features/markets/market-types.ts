export type AssetSummary = {
  symbol: string
  name: string
  exchange: string
  type: string
  currency: string
}

export type MarketQuote = {
  symbol: string
  currentPrice: number
  change: number
  percentChange: number
  open: number
  high: number
  low: number
  previousClose: number
  timestamp: string
}

export type HistoricalPrice = {
  time: string
  open: number
  high: number
  low: number
  close: number
  volume: number
}

export type Timeframe = '1D' | '1W' | '1M' | '3M' | '1Y'
