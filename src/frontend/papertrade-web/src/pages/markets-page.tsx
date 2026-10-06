import { Link } from 'react-router'
import { AppNav } from '../components/app-nav'
import { Brand } from '../components/brand'
import { useMarketSearch } from '../features/markets/market-queries'
import { useDebouncedValue } from '../hooks/use-debounced-value'
import { ApiError } from '../lib/api-client'
import { useState } from 'react'
import { marketDetailPath } from '../features/markets/market-path'

const popularSymbols = ['AAPL', 'MSFT', 'NVDA', 'EUR/USD', 'USD/JPY', 'XAU/USD', 'XAG/USD']

export function MarketsPage() {
  const [query, setQuery] = useState('')
  const debouncedQuery = useDebouncedValue(query.trim(), 300)
  const search = useMarketSearch(debouncedQuery)
  const error = search.error instanceof ApiError
    ? search.error.message
    : search.isError
      ? 'Market search is temporarily unavailable.'
      : null

  return (
    <main className="min-h-screen px-6 py-8">
      <div className="mx-auto max-w-6xl">
        <header className="flex flex-col gap-5 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <Brand />
            <h1 className="mt-2 text-3xl font-bold text-white">Markets</h1>
          </div>
          <AppNav />
        </header>

        <section className="mt-10">
          <label htmlFor="market-search" className="text-sm font-medium text-slate-200">Find a market</label>
          <input
            id="market-search"
            type="search"
            value={query}
            onChange={(event) => setQuery(event.target.value)}
            placeholder="Search stocks, FX, gold, or silver"
            className="mt-2 w-full rounded-xl border border-slate-700 bg-slate-900 px-4 py-3 text-white outline-none transition placeholder:text-slate-500 focus:border-emerald-400"
          />
          <p className="mt-2 text-sm text-slate-500">Enter at least two characters.</p>
        </section>

        {search.isFetching && <p className="mt-6 text-slate-400">Searching markets...</p>}
        {error && <p role="alert" className="mt-6 rounded-lg border border-red-900 bg-red-950/50 p-3 text-red-300">{error}</p>}

        {search.data && (
          <section className="mt-6 overflow-hidden rounded-2xl border border-slate-800 bg-slate-900">
            {search.data.length === 0 ? (
              <p className="p-6 text-slate-400">No matching markets found.</p>
            ) : search.data.map((asset) => (
              <Link
                key={`${asset.exchange}-${asset.symbol}`}
                to={marketDetailPath(asset.symbol)}
                className="flex items-center justify-between border-b border-slate-800 p-4 last:border-0 hover:bg-slate-800/60"
              >
                <div>
                  <p className="font-semibold text-white">{asset.symbol}</p>
                  <p className="mt-1 text-sm text-slate-400">{asset.displayName}</p>
                </div>
                <p className="text-sm text-slate-500">{asset.assetClass.toUpperCase()} · {asset.exchange} · {asset.quoteCurrency}</p>
              </Link>
            ))}
          </section>
        )}

        {!debouncedQuery && (
          <section className="mt-10">
            <h2 className="text-xl font-semibold text-white">Popular symbols</h2>
            <div className="mt-4 flex flex-wrap gap-3">
              {popularSymbols.map((symbol) => (
                <Link key={symbol} to={marketDetailPath(symbol)} className="rounded-xl border border-slate-700 bg-slate-900 px-5 py-3 font-semibold text-white hover:border-emerald-400">
                  {symbol}
                </Link>
              ))}
            </div>
          </section>
        )}
      </div>
    </main>
  )
}
