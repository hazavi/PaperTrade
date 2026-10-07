import type { Instrument } from '../markets/market-types'

export type PriceAlert = {
  id: string
  instrumentId: string
  instrument: Instrument
  symbol: string
  direction: 'above' | 'below'
  targetPrice: number
  metric: 'price' | 'percentChange' | 'volume' | 'sma20'
  isActive: boolean
  createdAt: string
  triggeredAt: string | null
}

export type Notification = {
  id: string
  title: string
  message: string
  isRead: boolean
  createdAt: string
}
