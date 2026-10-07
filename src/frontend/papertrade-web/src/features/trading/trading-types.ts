import type { Instrument } from '../markets/market-types'

export type Position = {
  id: string
  instrumentId: string
  instrument: Instrument
  symbol: string
  quantity: number
  averageEntryPrice: number
  currentPrice: number
  marketValue: number
  unrealizedPnl: number
  returnPercentage: number
  updatedAt: string
  marginReserved: number
  notionalValue: number
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
  usedMargin: number
  availableMargin: number
  marginLevelPercent: number | null
  maintenanceWarning: boolean
  grossExposure: number
}

export type Order = {
  id: string
  instrumentId: string
  instrument: Instrument
  portfolioId: string
  symbol: string
  side: 'buy' | 'sell'
  type: 'market' | 'limit' | 'stop' | 'bracket'
  quantity: number
  requestedPrice: number
  executedPrice: number | null
  totalValue: number | null
  status: 'pending' | 'partially_filled' | 'filled' | 'rejected' | 'cancelled' | 'expired'
  filledQuantity: number
  parentOrderId: string | null
  expiresAt: string | null
  closedAt: string | null
  createdAt: string
  executedAt: string | null
}

export type CreateOrder = {
  symbol: string
  side: 'buy' | 'sell'
  type: Order['type']
  quantity: number
  price?: number
  takeProfit?: number
  stopLoss?: number
  expiresAt?: string
}

export type Execution = {
  id: string
  orderId: string
  symbol: string
  side: 'buy' | 'sell'
  quantity: number
  quotePrice: number
  price: number
  totalValue: number
  fee: number
  realizedPnl: number
  executedAt: string
}

export type OrderExecution = {
  status: string
  order: Order
  cashBalance: number
  ownedQuantity: number
}

export type RiskLimits = { maxDailyLossPercent: number | null; maxPositionConcentrationPercent: number | null }
export type SizeResult = { quantity: number; riskBudgetUsd: number; riskPerUnitUsd: number; estimatedRiskUsd: number; rewardRiskRatio: number | null; stopDistancePips: number; tickValueUsd: number; lotSize: number; warnings: string[] }
export type Snapshot = { recordedAt: string; equity: number; cash: number; realizedPnl: number; unrealizedPnl: number; drawdownPercent: number }
export type PeriodPnl = { period: string; pnl: number }
export type Performance = { history: Snapshot[]; maxDrawdownPercent: number; daily: PeriodPnl[]; weekly: PeriodPnl[]; monthly: PeriodPnl[] }
export type JournalEntry = { id: string; orderId: string; note: string; createdAt: string; updatedAt: string }
export type MarginSettings = { enabled: boolean; equityLeverage: number; forexLeverage: number; metalLeverage: number; commodityLeverage: number; indexLeverage: number; cryptoLeverage: number }
export type FinancingCharge = { id: string; symbol: string; chargedAt: string; amount: number }
