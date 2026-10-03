import { apiRequest } from '../../lib/api-client'
import type {
  CreateOrder,
  Order,
  OrderExecution,
  Portfolio,
} from './trading-types'

export function getPortfolio() {
  return apiRequest<Portfolio>('/api/portfolio')
}

export function getOrders() {
  return apiRequest<Order[]>('/api/orders')
}

export function createOrder(order: CreateOrder) {
  return apiRequest<OrderExecution>('/api/orders', {
    method: 'POST',
    body: JSON.stringify(order),
  })
}
