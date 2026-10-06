export type Instrument = {
  id: string
  symbol: string
  displayName: string
  assetClass: string
  exchange: string
  baseCurrency: string | null
  quoteCurrency: string
  pricePrecision: number
  quantityPrecision: number
  tickSize: number
  minimumOrderSize: number
  marketTimeZone: string
  tradingSession: string
  isTradable: boolean
}

export type AssetSummary = Instrument

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
