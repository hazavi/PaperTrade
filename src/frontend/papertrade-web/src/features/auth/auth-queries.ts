import { useQuery } from '@tanstack/react-query'
import { getCurrentUser } from './auth-api'

export const authKeys = {
  currentUser: ['auth', 'current-user'] as const,
}

export function useCurrentUser() {
  return useQuery({
    queryKey: authKeys.currentUser,
    queryFn: getCurrentUser,
    staleTime: 60_000,
    retry: 1,
  })
}