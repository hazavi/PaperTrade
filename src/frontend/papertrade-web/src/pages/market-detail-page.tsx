import { useState } from 'react'
import { Link, useParams } from 'react-router'
import { AppNav } from '../components/app-nav'
import { OrderTicket } from '../components/order-ticket'
import { PriceChart } from '../components/price-chart'
import { useMarketHistory, useMarketQuote } from '../features/markets/market-queries'
import type { Timeframe } from '../features/markets/market-types'
import { useRealtimeSymbol } from '../features/realtime/use-realtime-symbol'
import { ApiError } from '../lib/api-client'
import { formatMoney } from '../lib/format'

const timeframes: Timeframe[] = ['1D', '1W', '1M', '3M', '1Y']

export function MarketDetailPage() {
  const { symbol: rawSymbol = '' } = useParams()
  const symbol = decodeURIComponent(rawSymbol).toUpperCase()
  const [timeframe, setTimeframe] = useState<Timeframe>('1M')
  const [riskLevels, setRiskLevels] = useState<{ takeProfit: number | null; stopLoss: number | null }>({ takeProfit: null, stopLoss: null })
  useRealtimeSymbol(symbol)
  const quote = useMarketQuote(symbol)
  const history = useMarketHistory(symbol, timeframe)
  const queryError = quote.error ?? history.error
  const error = queryError instanceof ApiError ? queryError.message : queryError ? 'Market data is temporarily unavailable.' : null
  const isPositive = (quote.data?.change ?? 0) >= 0

  return (
    <main className="market-workspace min-h-screen px-4 py-5 lg:px-6">
      <div className="mx-auto max-w-[1600px]">
        <header className="flex flex-col gap-4 xl:flex-row xl:items-center xl:justify-between">
          <div className="flex items-center gap-4">
            <Link to="/markets" aria-label="Back to markets" className="market-back">←</Link>
            <div>
              <div className="flex items-center gap-3"><h1 className="text-2xl font-bold text-white">{symbol}</h1><span className="market-status"><i /> Market</span></div>
              {quote.data && <p className="mt-1 text-sm text-slate-400">Live paper-trading workspace · {formatMoney(quote.data.currentPrice)}</p>}
            </div>
          </div>
          <AppNav />
        </header>

        {error && <p role="alert" className="mt-5 rounded-lg border border-red-900 bg-red-950/50 p-3 text-red-300">{error}</p>}
        {quote.isLoading && <p className="mt-8 text-slate-400">Loading quote...</p>}

        {quote.data && (
          <section className="quote-strip" aria-label={`${symbol} quote`}>
            <div className="quote-strip__price"><span>{symbol}</span><strong>{formatMoney(quote.data.currentPrice)}</strong><em className={isPositive ? 'is-positive' : 'is-negative'}>{isPositive ? '+' : ''}{quote.data.change.toFixed(2)} ({isPositive ? '+' : ''}{quote.data.percentChange.toFixed(2)}%)</em></div>
            <QuoteMetric label="Open" value={formatMoney(quote.data.open)} />
            <QuoteMetric label="High" value={formatMoney(quote.data.high)} />
            <QuoteMetric label="Low" value={formatMoney(quote.data.low)} />
            <QuoteMetric label="Prev. close" value={formatMoney(quote.data.previousClose)} />
            <div className="quote-strip__actions"><Link to={`/watchlists?symbol=${encodeURIComponent(symbol)}`}>☆ Watchlist</Link><Link to={`/alerts?symbol=${encodeURIComponent(symbol)}`}>Alert</Link></div>
          </section>
        )}

        <div className="trading-workspace-grid">
          <section className="chart-panel">
            <div className="chart-panel__topbar">
              <div><p className="chart-panel__title">{symbol} · US Equity</p><p className="chart-panel__subtitle">Interactive market chart</p></div>
              <div className="timeframe-switch" role="group" aria-label="Chart timeframe">
                {timeframes.map((value) => <button key={value} type="button" onClick={() => setTimeframe(value)} aria-pressed={timeframe === value} className={timeframe === value ? 'is-active' : ''}>{value}</button>)}
              </div>
            </div>
            {history.isLoading
              ? <p className="py-64 text-center text-slate-400">Loading price history...</p>
              : history.data?.length
                ? <PriceChart prices={history.data} takeProfit={riskLevels.takeProfit} stopLoss={riskLevels.stopLoss} />
                : !error && <p className="py-64 text-center text-slate-400">No historical prices are available.</p>}
          </section>

          {quote.data && <OrderTicket symbol={symbol} price={quote.data.currentPrice} onRiskLevelsChange={setRiskLevels} />}
        </div>
      </div>
    </main>
  )
}

function QuoteMetric({ label, value }: { label: string; value: string }) {
  return <div className="quote-strip__metric"><span>{label}</span><strong>{value}</strong></div>
}
