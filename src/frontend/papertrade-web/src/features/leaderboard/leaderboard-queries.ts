import { useQuery } from '@tanstack/react-query'
import { getLeaderboard } from './leaderboard-api'

export const useLeaderboard = (page: number) => useQuery({
  queryKey: ['leaderboard', page],
  queryFn: () => getLeaderboard(page),
  staleTime: 60_000,
})
