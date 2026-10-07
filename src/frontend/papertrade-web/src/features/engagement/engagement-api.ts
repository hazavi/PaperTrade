import { apiRequest } from '../../lib/api-client'
import type { Notification, PriceAlert } from './engagement-types'

export const getAlerts = () => apiRequest<PriceAlert[]>('/api/alerts')
export const createAlert = (request: { symbol: string; direction: string; targetPrice: number; metric: string }) =>
  apiRequest<PriceAlert>('/api/alerts', { method: 'POST', body: JSON.stringify(request) })
export const deleteAlert = (id: string) => apiRequest<void>(`/api/alerts/${id}`, { method: 'DELETE' })
export const getNotifications = () => apiRequest<Notification[]>('/api/notifications')
export const markNotificationRead = (id: string) =>
  apiRequest<void>(`/api/notifications/${id}/read`, { method: 'POST' })
export const getEmailPreference = () => apiRequest<{ emailAlertsEnabled: boolean }>('/api/notification-preferences')
export const setEmailPreference = (emailAlertsEnabled: boolean) => apiRequest<{ emailAlertsEnabled: boolean }>('/api/notification-preferences', { method: 'PUT', body: JSON.stringify({ emailAlertsEnabled }) })
