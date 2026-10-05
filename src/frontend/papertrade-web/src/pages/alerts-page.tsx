import { type FormEvent, useState } from 'react'
import { useSearchParams } from 'react-router'
import { AppNav } from '../components/app-nav'
import { Brand } from '../components/brand'
import { useAlerts, useCreateAlert, useDeleteAlert } from '../features/engagement/engagement-queries'
import { ApiError } from '../lib/api-client'
import { formatMoney } from '../lib/format'

export function AlertsPage() {
  const [params] = useSearchParams()
  const [symbol, setSymbol] = useState(params.get('symbol')?.toUpperCase() ?? '')
  const [direction, setDirection] = useState('above')
  const [targetPrice, setTargetPrice] = useState('')
  const alerts = useAlerts()
  const create = useCreateAlert()
  const remove = useDeleteAlert()

  function submit(event: FormEvent) {
    event.preventDefault()
    create.mutate({ symbol: symbol.trim().toUpperCase(), direction, targetPrice: Number(targetPrice) }, {
      onSuccess: () => { setTargetPrice(''); setSymbol('') },
    })
  }
  const error = create.error instanceof ApiError ? create.error.message : create.isError ? 'Could not create alert.' : null

  return <main className="min-h-screen px-6 py-8"><div className="mx-auto max-w-6xl">
    <header className="flex flex-col gap-5 sm:flex-row sm:items-center sm:justify-between"><div><Brand /><h1 className="mt-2 text-3xl font-bold text-white">Price alerts</h1></div><AppNav /></header>
    <form onSubmit={submit} className="mt-10 grid gap-3 rounded-2xl border border-slate-800 bg-slate-900 p-5 sm:grid-cols-[1fr_1fr_1fr_auto]">
      <label className="sr-only" htmlFor="alert-symbol">Symbol</label><input id="alert-symbol" required value={symbol} onChange={(event) => setSymbol(event.target.value)} placeholder="AAPL" className="rounded-lg border border-slate-700 bg-slate-950 px-4 py-3 uppercase text-white" />
      <label className="sr-only" htmlFor="alert-direction">Direction</label><select id="alert-direction" value={direction} onChange={(event) => setDirection(event.target.value)} className="rounded-lg border border-slate-700 bg-slate-950 px-4 py-3 text-white"><option value="above">Above</option><option value="below">Below</option></select>
      <label className="sr-only" htmlFor="target-price">Target price</label><input id="target-price" required type="number" min="0.000001" step="0.000001" value={targetPrice} onChange={(event) => setTargetPrice(event.target.value)} placeholder="Target price" className="rounded-lg border border-slate-700 bg-slate-950 px-4 py-3 text-white" />
      <button type="submit" disabled={create.isPending} className="rounded-lg bg-emerald-400 px-5 py-3 font-semibold text-slate-950 disabled:opacity-50">Create alert</button>
    </form>
    {error && <p role="alert" className="mt-3 text-red-300">{error}</p>}
    <section className="mt-8 overflow-hidden rounded-2xl border border-slate-800 bg-slate-900">
      {alerts.isLoading && <p className="p-5 text-slate-400">Loading alerts...</p>}
      {alerts.data?.length === 0 && <p className="p-5 text-slate-400">No price alerts.</p>}
      {alerts.data?.map((alert) => <div key={alert.id} className="flex items-center gap-4 border-b border-slate-800 p-5 last:border-0"><div><p className="font-semibold text-white">{alert.symbol} {alert.direction} {formatMoney(alert.targetPrice)}</p><p className="mt-1 text-sm text-slate-500">{alert.isActive ? 'Active' : `Triggered ${new Date(alert.triggeredAt!).toLocaleString()}`}</p></div><button type="button" onClick={() => remove.mutate(alert.id)} className="ml-auto rounded-lg px-3 py-2 text-sm text-slate-400 hover:text-red-300">Delete</button></div>)}
    </section>
  </div></main>
}
