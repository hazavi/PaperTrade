import { HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr'
import { useQueryClient } from '@tanstack/react-query'
import { useEffect } from 'react'
import { apiBaseUrl } from '../../lib/api-client'
import { marketKeys } from '../markets/market-queries'
import type { MarketQuote } from '../markets/market-types'

const connection = new HubConnectionBuilder()
  .withUrl(`${apiBaseUrl}/hubs/market`, { withCredentials: true })
  .withAutomaticReconnect()
  .build()

const subscriptions = new Map<string, number>()
let startPromise: Promise<void> | null = null

async function ensureStarted() {
  if (connection.state === HubConnectionState.Connected) return
  startPromise ??= connection.start().finally(() => { startPromise = null })
  await startPromise
}

export function useRealtimeSymbol(symbol: string) {
  const queryClient = useQueryClient()

  useEffect(() => {
    const normalized = symbol.trim().toUpperCase()
    if (!normalized) return

    const onQuote = (quote: MarketQuote) => {
      queryClient.setQueryData(marketKeys.quote(quote.symbol), quote)
      queryClient.invalidateQueries({ queryKey: ['trading', 'portfolio'] })
    }
    const onNotification = () =>
      queryClient.invalidateQueries({ queryKey: ['engagement', 'notifications'] })

    connection.on('QuoteUpdated', onQuote)
    connection.on('NotificationReceived', onNotification)
    const count = subscriptions.get(normalized) ?? 0
    subscriptions.set(normalized, count + 1)
    if (count === 0) {
      ensureStarted()
        .then(() => connection.invoke('Subscribe', normalized))
        .catch(() => undefined)
    }

    return () => {
      connection.off('QuoteUpdated', onQuote)
      connection.off('NotificationReceived', onNotification)
      const remaining = (subscriptions.get(normalized) ?? 1) - 1
      if (remaining <= 0) {
        subscriptions.delete(normalized)
        if (connection.state === HubConnectionState.Connected)
          void connection.invoke('Unsubscribe', normalized)
      } else subscriptions.set(normalized, remaining)
    }
  }, [queryClient, symbol])
}

export function useRealtimeNotifications(enabled = true) {
  const queryClient = useQueryClient()
  useEffect(() => {
    if (!enabled) return

    const onNotification = () =>
      queryClient.invalidateQueries({ queryKey: ['engagement', 'notifications'] })
    connection.on('NotificationReceived', onNotification)
    ensureStarted().catch(() => undefined)
    return () => connection.off('NotificationReceived', onNotification)
  }, [enabled, queryClient])
}
