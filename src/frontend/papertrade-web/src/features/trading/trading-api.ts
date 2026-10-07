import { apiRequest } from '../../lib/api-client'
import type {
  CreateOrder,
  Order,
  OrderExecution,
  Execution,
  Portfolio,
} from './trading-types'

export function getPortfolio() {
  return apiRequest<Portfolio>('/api/portfolio')
}

export function getOrders() {
  return apiRequest<Order[]>('/api/orders')
}

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
