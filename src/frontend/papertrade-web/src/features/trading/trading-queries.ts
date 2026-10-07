import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { cancelOrder, createOrder, getExecutions, getOrders, getPortfolio } from './trading-api'

export const tradingKeys = {
  portfolio: ['trading', 'portfolio'] as const,
  orders: ['trading', 'orders'] as const,
  executions: ['trading', 'executions'] as const,
}

export function usePortfolio() {
  return useQuery({
    queryKey: tradingKeys.portfolio,
    queryFn: getPortfolio,
    staleTime: 15_000,
    refetchInterval: 5_000,
    retry: 1,
  })
}

export function useOrders() {
  return useQuery({
    queryKey: tradingKeys.orders,
    queryFn: getOrders,
    staleTime: 15_000,
    refetchInterval: 5_000,
    retry: 1,
  })
}

export function useExecutions() {
  return useQuery({ queryKey: tradingKeys.executions, queryFn: getExecutions,
    staleTime: 15_000, refetchInterval: 5_000, retry: 1 })
}

export function useCreateOrder() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: createOrder,
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: tradingKeys.portfolio }),
        queryClient.invalidateQueries({ queryKey: tradingKeys.orders }),
        queryClient.invalidateQueries({ queryKey: tradingKeys.executions }),
      ])
    },
  })
}

export function useCancelOrder() {
  const queryClient = useQueryClient()
  return useMutation({ mutationFn: cancelOrder, onSuccess: async () => {
    await queryClient.invalidateQueries({ queryKey: tradingKeys.orders })
  } })
}
