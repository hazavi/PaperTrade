import { type FormEvent, useState } from 'react'
import { Link, useSearchParams } from 'react-router'
import { AppNav } from '../components/app-nav'
import { Brand } from '../components/brand'
import { useMarketQuote } from '../features/markets/market-queries'
import {
  useAddWatchlistItem,
  useCreateWatchlist,
  useRemoveWatchlistItem,
  useWatchlists,
} from '../features/watchlists/watchlist-queries'
import type { Watchlist } from '../features/watchlists/watchlist-types'
import { ApiError } from '../lib/api-client'
import { useRealtimeSymbol } from '../features/realtime/use-realtime-symbol'

function errorMessage(error: unknown) {
  return error instanceof ApiError ? error.message : 'The request could not be completed.'
}

export function WatchlistsPage() {
  const [searchParams] = useSearchParams()
  const suggestedSymbol = searchParams.get('symbol')?.toUpperCase() ?? ''
  const [name, setName] = useState('')
  const watchlists = useWatchlists()
  const create = useCreateWatchlist()

  function submit(event: FormEvent) {
    event.preventDefault()
    const trimmedName = name.trim()
    if (!trimmedName) return
    create.mutate(trimmedName, { onSuccess: () => setName('') })
  }

  return (
    <main className="min-h-screen px-6 py-8">
      <div className="mx-auto max-w-6xl">
        <header className="flex flex-col gap-5 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <Brand />
            <h1 className="mt-2 text-3xl font-bold text-white">Watchlists</h1>
          </div>
          <AppNav />
        </header>

        <form onSubmit={submit} className="mt-10 flex max-w-xl gap-3">
          <label htmlFor="watchlist-name" className="sr-only">Watchlist name</label>
          <input id="watchlist-name" value={name} onChange={(event) => setName(event.target.value)} maxLength={100} placeholder="New watchlist name" className="min-w-0 flex-1 rounded-xl border border-slate-700 bg-slate-900 px-4 py-3 text-white outline-none focus:border-emerald-400" />
          <button type="submit" disabled={create.isPending || !name.trim()} className="rounded-xl bg-emerald-400 px-5 py-3 font-semibold text-slate-950 disabled:opacity-50">{create.isPending ? 'Creating...' : 'Create'}</button>
        </form>
        {create.isError && <p role="alert" className="mt-3 text-sm text-red-300">{errorMessage(create.error)}</p>}

        {watchlists.isLoading && <p className="mt-8 text-slate-400">Loading watchlists...</p>}
        {watchlists.isError && <p role="alert" className="mt-8 rounded-lg border border-red-900 bg-red-950/50 p-3 text-red-300">{errorMessage(watchlists.error)}</p>}
        {watchlists.data?.length === 0 && <p className="mt-8 rounded-2xl border border-slate-800 bg-slate-900 p-6 text-slate-400">Create your first watchlist to track stocks.</p>}

        <section className="mt-8 grid gap-6">
          {watchlists.data?.map((watchlist) => <WatchlistCard key={watchlist.id} watchlist={watchlist} suggestedSymbol={suggestedSymbol} />)}
        </section>
      </div>
    </main>
  )
}

function WatchlistCard({ watchlist, suggestedSymbol }: { watchlist: Watchlist; suggestedSymbol: string }) {
  const [symbol, setSymbol] = useState(suggestedSymbol)
  const add = useAddWatchlistItem()

  function submit(event: FormEvent) {
    event.preventDefault()
    const normalized = symbol.trim().toUpperCase()
    if (!normalized) return
    add.mutate({ id: watchlist.id, symbol: normalized }, { onSuccess: () => setSymbol('') })
  }

  return (
    <article className="overflow-hidden rounded-2xl border border-slate-800 bg-slate-900">
      <div className="flex flex-col gap-4 border-b border-slate-800 p-5 sm:flex-row sm:items-center sm:justify-between">
        <div><h2 className="text-xl font-semibold text-white">{watchlist.name}</h2><p className="mt-1 text-sm text-slate-500">{watchlist.items.length} {watchlist.items.length === 1 ? 'symbol' : 'symbols'}</p></div>
        <form onSubmit={submit} className="flex gap-2">
          <label htmlFor={`symbol-${watchlist.id}`} className="sr-only">Symbol</label>
          <input id={`symbol-${watchlist.id}`} value={symbol} onChange={(event) => setSymbol(event.target.value)} maxLength={32} placeholder="AAPL" className="w-32 rounded-lg border border-slate-700 bg-slate-950 px-3 py-2 uppercase text-white outline-none focus:border-emerald-400" />
          <button type="submit" disabled={add.isPending || !symbol.trim()} className="rounded-lg border border-emerald-500 px-4 py-2 font-medium text-emerald-300 disabled:opacity-50">Add</button>
        </form>
      </div>
      {add.isError && <p role="alert" className="border-b border-slate-800 p-3 text-sm text-red-300">{errorMessage(add.error)}</p>}
      {watchlist.items.length === 0 ? <p className="p-5 text-slate-500">No symbols in this watchlist.</p> : watchlist.items.map((item) => <WatchlistRow key={item.id} watchlistId={watchlist.id} symbol={item.symbol} />)}
    </article>
  )
}

function WatchlistRow({ watchlistId, symbol }: { watchlistId: string; symbol: string }) {
  useRealtimeSymbol(symbol)
  const quote = useMarketQuote(symbol)
  const remove = useRemoveWatchlistItem()
  const positive = (quote.data?.change ?? 0) >= 0

  return (
    <div className="flex items-center justify-between gap-4 border-b border-slate-800 p-4 last:border-0">
      <Link to={`/markets/${encodeURIComponent(symbol)}`} className="font-semibold text-white hover:text-emerald-300">{symbol}</Link>
      <div className="ml-auto text-right">
        {quote.isLoading ? <p className="text-sm text-slate-500">Loading...</p> : quote.data ? <><p className="font-medium text-white">${quote.data.currentPrice.toFixed(2)}</p><p className={`text-sm ${positive ? 'text-emerald-400' : 'text-red-400'}`}>{positive ? '+' : ''}{quote.data.percentChange.toFixed(2)}%</p></> : <p className="text-sm text-slate-500">Price unavailable</p>}
      </div>
      <button type="button" onClick={() => remove.mutate({ id: watchlistId, symbol })} disabled={remove.isPending} className="rounded-lg px-3 py-2 text-sm text-slate-400 hover:bg-slate-800 hover:text-red-300 disabled:opacity-50">Remove</button>
    </div>
  )
}
