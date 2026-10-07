import { apiRequest } from '../../lib/api-client'
import type {
  CreateOrder,
  Order,
  OrderExecution,
  Execution,
  Portfolio,
  RiskLimits, SizeResult, Performance, JournalEntry, MarginSettings, FinancingCharge,
} from './trading-types'

export function getPortfolio() {
  return apiRequest<Portfolio>('/api/portfolio')
}

export function getOrders() {
  return apiRequest<Order[]>('/api/orders')
}

export const getRiskLimits = () => apiRequest<RiskLimits>('/api/analytics/risk-limits')
export const saveRiskLimits = (limits: RiskLimits) => apiRequest<RiskLimits>('/api/analytics/risk-limits', { method: 'PUT', body: JSON.stringify(limits) })
export const calculateSize = (request: { symbol: string; entryPrice: number; stopPrice: number; riskPercent: number; takeProfitPrice?: number }) =>
  apiRequest<SizeResult>('/api/analytics/size', { method: 'POST', body: JSON.stringify(request) })
export const getPerformance = () => apiRequest<Performance>('/api/analytics/performance')
export const getJournal = () => apiRequest<JournalEntry[]>('/api/analytics/journal')
export const saveJournal = (orderId: string, note: string) => apiRequest<JournalEntry>(`/api/analytics/journal/${encodeURIComponent(orderId)}`, { method: 'PUT', body: JSON.stringify({ note }) })
export const deleteJournal = (orderId: string) => apiRequest<void>(`/api/analytics/journal/${encodeURIComponent(orderId)}`, { method: 'DELETE' })
export const getMarginSettings = () => apiRequest<MarginSettings>('/api/portfolio/margin')
export const saveMarginSettings = (settings: MarginSettings) => apiRequest<MarginSettings>('/api/portfolio/margin', { method: 'PUT', body: JSON.stringify(settings) })
export const getFinancingCharges = () => apiRequest<FinancingCharge[]>('/api/portfolio/margin/charges')

export function getExecutions() {
  return apiRequest<Execution[]>('/api/orders/executions')
}

export function cancelOrder(id: string) {
  return apiRequest<void>(`/api/orders/${encodeURIComponent(id)}`, { method: 'DELETE' })
}

export function createOrder(order: CreateOrder) {
  return apiRequest<OrderExecution>('/api/orders', {
    method: 'POST',
    body: JSON.stringify(order),
  })
}
