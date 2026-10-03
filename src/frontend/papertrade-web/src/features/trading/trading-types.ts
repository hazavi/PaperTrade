export type Position = {
  id: string
  symbol: string
  quantity: number
  averageEntryPrice: number
  currentPrice: number
  marketValue: number
  unrealizedPnl: number
  returnPercentage: number
  updatedAt: string
}

export type Portfolio = {
  id: string
  name: string
  cashBalance: number
  initialBalance: number
  marketValue: number
  portfolioValue: number
  unrealizedPnl: number
  realizedPnl: number
  totalReturnPercentage: number
  positions: Position[]
}

export type Order = {
  id: string
  portfolioId: string
  symbol: string
  side: 'buy' | 'sell'
  type: 'market'
  quantity: number
  requestedPrice: number
  executedPrice: number | null
  totalValue: number | null
  status: 'pending' | 'filled' | 'rejected' | 'cancelled'
  createdAt: string
  executedAt: string | null
}

export type CreateOrder = {
  symbol: string
  side: 'buy' | 'sell'
  type: 'market'
  quantity: number
}

export type OrderExecution = {
  status: string
  order: Order
  cashBalance: number
  ownedQuantity: number
}
