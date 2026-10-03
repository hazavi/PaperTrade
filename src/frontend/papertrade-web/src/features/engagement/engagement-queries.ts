import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { createAlert, deleteAlert, getAlerts, getNotifications, markNotificationRead } from './engagement-api'

export const engagementKeys = {
  alerts: ['engagement', 'alerts'] as const,
  notifications: ['engagement', 'notifications'] as const,
}
export const useAlerts = () => useQuery({ queryKey: engagementKeys.alerts, queryFn: getAlerts })
export const useNotifications = () => useQuery({ queryKey: engagementKeys.notifications, queryFn: getNotifications })
export function useCreateAlert() {
  const client = useQueryClient()
  return useMutation({ mutationFn: createAlert, onSuccess: () => client.invalidateQueries({ queryKey: engagementKeys.alerts }) })
}
export function useDeleteAlert() {
  const client = useQueryClient()
  return useMutation({ mutationFn: deleteAlert, onSuccess: () => client.invalidateQueries({ queryKey: engagementKeys.alerts }) })
}
export function useMarkNotificationRead() {
  const client = useQueryClient()
  return useMutation({ mutationFn: markNotificationRead, onSuccess: () => client.invalidateQueries({ queryKey: engagementKeys.notifications }) })
}
