import type { Instrument } from '../markets/market-types'

export type WatchlistItem = {
  id: string
  instrumentId: string
  instrument: Instrument
  symbol: string
  addedAt: string
}

export type Watchlist = {
  id: string
  name: string
  createdAt: string
  items: WatchlistItem[]
}
