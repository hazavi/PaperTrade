import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { createOrder, getOrders, getPortfolio } from './trading-api'

export const tradingKeys = {
  portfolio: ['trading', 'portfolio'] as const,
  orders: ['trading', 'orders'] as const,
}

export function usePortfolio() {
  return useQuery({
    queryKey: tradingKeys.portfolio,
    queryFn: getPortfolio,
    staleTime: 15_000,
    retry: 1,
  })
}

export function useOrders() {
  return useQuery({
    queryKey: tradingKeys.orders,
    queryFn: getOrders,
    staleTime: 15_000,
    retry: 1,
  })
}

export function useCreateOrder() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: createOrder,
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: tradingKeys.portfolio }),
        queryClient.invalidateQueries({ queryKey: tradingKeys.orders }),
      ])
    },
  })
}
