export type WatchlistItem = {
  id: string
  symbol: string
  addedAt: string
}

export type Watchlist = {
  id: string
  name: string
  createdAt: string
  items: WatchlistItem[]
}
