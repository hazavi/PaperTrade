import { useState } from 'react'
import { Link, useParams } from 'react-router'
import { AppNav } from '../components/app-nav'
import { PriceChart } from '../components/price-chart'
import { useMarketHistory, useMarketQuote } from '../features/markets/market-queries'
import type { Timeframe } from '../features/markets/market-types'
import { ApiError } from '../lib/api-client'

const timeframes: Timeframe[] = ['1D', '1W', '1M', '3M', '1Y']

function formatMoney(value: number) {
  return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(value)
}

export function MarketDetailPage() {
  const { symbol: rawSymbol = '' } = useParams()
  const symbol = decodeURIComponent(rawSymbol).toUpperCase()
  const [timeframe, setTimeframe] = useState<Timeframe>('1M')
  const quote = useMarketQuote(symbol)
  const history = useMarketHistory(symbol, timeframe)
  const queryError = quote.error ?? history.error
  const error = queryError instanceof ApiError
    ? queryError.message
    : queryError
      ? 'Market data is temporarily unavailable.'
      : null
  const isPositive = (quote.data?.change ?? 0) >= 0

  return (
    <main className="min-h-screen px-6 py-8">
      <div className="mx-auto max-w-6xl">
        <header className="flex flex-col gap-5 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <Link to="/markets" className="text-sm text-emerald-400 hover:text-emerald-300">← Back to markets</Link>
            <h1 className="mt-2 text-3xl font-bold text-white">{symbol}</h1>
          </div>
          <AppNav />
        </header>

        {error && <p role="alert" className="mt-8 rounded-lg border border-red-900 bg-red-950/50 p-3 text-red-300">{error}</p>}
        {quote.isLoading && <p className="mt-8 text-slate-400">Loading quote...</p>}

        {quote.data && (
          <section className="mt-8 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
            <QuoteCard label="Current price" value={formatMoney(quote.data.currentPrice)} />
            <QuoteCard label="Change" value={`${isPositive ? '+' : ''}${quote.data.change.toFixed(2)} (${isPositive ? '+' : ''}${quote.data.percentChange.toFixed(2)}%)`} valueClass={isPositive ? 'text-emerald-400' : 'text-red-400'} />
            <QuoteCard label="Day range" value={`${formatMoney(quote.data.low)} – ${formatMoney(quote.data.high)}`} />
            <QuoteCard label="Previous close" value={formatMoney(quote.data.previousClose)} />
          </section>
        )}

        <section className="mt-6 rounded-2xl border border-slate-800 bg-slate-900 p-5">
          <div className="mb-4 flex flex-wrap gap-2">
            {timeframes.map((value) => (
              <button key={value} type="button" onClick={() => setTimeframe(value)} className={`rounded-lg px-3 py-2 text-sm font-medium ${timeframe === value ? 'bg-emerald-400 text-slate-950' : 'bg-slate-800 text-slate-300 hover:text-white'}`}>
                {value}
              </button>
            ))}
          </div>
          {history.isLoading ? <p className="py-32 text-center text-slate-400">Loading price history...</p> : history.data?.length ? <PriceChart prices={history.data} /> : !error && <p className="py-32 text-center text-slate-400">No historical prices are available.</p>}
        </section>

        <section className="mt-6 flex flex-wrap items-center gap-3 rounded-2xl border border-slate-800 bg-slate-900 p-5">
          <button type="button" disabled className="rounded-lg bg-emerald-400 px-5 py-3 font-semibold text-slate-950 opacity-60">Buy</button>
          <button type="button" disabled className="rounded-lg bg-red-400 px-5 py-3 font-semibold text-slate-950 opacity-60">Sell</button>
          <Link to={`/watchlists?symbol=${encodeURIComponent(symbol)}`} className="rounded-lg border border-slate-700 px-5 py-3 font-semibold text-white hover:border-emerald-400">Add to watchlist</Link>
          <p className="text-sm text-slate-500">Order execution arrives in Week 3.</p>
        </section>
      </div>
    </main>
  )
}

function QuoteCard({ label, value, valueClass = 'text-white' }: { label: string; value: string; valueClass?: string }) {
  return <article className="rounded-2xl border border-slate-800 bg-slate-900 p-5"><p className="text-sm text-slate-400">{label}</p><p className={`mt-2 text-xl font-semibold ${valueClass}`}>{value}</p></article>
}
