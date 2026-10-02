import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  addWatchlistItem,
  createWatchlist,
  getWatchlists,
  removeWatchlistItem,
} from './watchlist-api'

export const watchlistKeys = {
  all: ['watchlists'] as const,
}

export function useWatchlists() {
  return useQuery({
    queryKey: watchlistKeys.all,
    queryFn: getWatchlists,
    staleTime: 30_000,
  })
}

export function useCreateWatchlist() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: createWatchlist,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: watchlistKeys.all }),
  })
}

export function useAddWatchlistItem() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, symbol }: { id: string; symbol: string }) =>
      addWatchlistItem(id, symbol),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: watchlistKeys.all }),
  })
}

export function useRemoveWatchlistItem() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, symbol }: { id: string; symbol: string }) =>
      removeWatchlistItem(id, symbol),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: watchlistKeys.all }),
  })
}
