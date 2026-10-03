export type PriceAlert = {
  id: string
  symbol: string
  direction: 'above' | 'below'
  targetPrice: number
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
