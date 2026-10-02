import { apiRequest } from '../../lib/api-client'
import type { Watchlist } from './watchlist-types'

export function getWatchlists() {
  return apiRequest<Watchlist[]>('/api/watchlists')
}

export function createWatchlist(name: string) {
  return apiRequest<Watchlist>('/api/watchlists', {
    method: 'POST',
    body: JSON.stringify({ name }),
  })
}

export function addWatchlistItem(id: string, symbol: string) {
  return apiRequest<Watchlist>(`/api/watchlists/${id}/assets`, {
    method: 'POST',
    body: JSON.stringify({ symbol }),
  })
}

export function removeWatchlistItem(id: string, symbol: string) {
  return apiRequest<void>(
    `/api/watchlists/${id}/assets/${encodeURIComponent(symbol)}`,
    { method: 'DELETE' },
  )
}
