import { apiRequest } from '../../lib/api-client'
import type { ChartLayoutState } from '../../components/price-chart'
import type { Timeframe } from './market-types'

export type SavedChartLayout = { id: string; name: string; symbol: string; timeframe: Timeframe; state: ChartLayoutState; updatedAt: string }
export type ChartLayoutInput = Omit<SavedChartLayout, 'id' | 'updatedAt'>

export const getChartLayouts = () => apiRequest<SavedChartLayout[]>('/api/chart-layouts')
export const createChartLayout = (value: ChartLayoutInput) => apiRequest<SavedChartLayout>('/api/chart-layouts', { method: 'POST', body: JSON.stringify(value) })
export const updateChartLayout = (id: string, value: ChartLayoutInput) => apiRequest<SavedChartLayout>(`/api/chart-layouts/${id}`, { method: 'PUT', body: JSON.stringify(value) })
export const deleteChartLayout = (id: string) => apiRequest<void>(`/api/chart-layouts/${id}`, { method: 'DELETE' })
