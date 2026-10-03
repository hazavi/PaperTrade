import { apiRequest } from '../../lib/api-client'

export type LeaderboardPage = {
  page: number
  pageSize: number
  totalCount: number
  entries: Array<{
    rank: number
    userId: string
    displayName: string
    portfolioValue: number
    returnPercentage: number
  }>
}

export const getLeaderboard = (page: number) =>
  apiRequest<LeaderboardPage>(`/api/leaderboard?page=${page}&pageSize=20`)
