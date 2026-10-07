import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { AppNav } from '../components/app-nav'
import { Brand } from '../components/brand'
import { deleteJournal, getJournal, getPerformance, getRiskLimits, saveJournal, saveRiskLimits } from '../features/trading/trading-api'
import { useOrders } from '../features/trading/trading-queries'
import type { PeriodPnl, Snapshot } from '../features/trading/trading-types'
import { apiBaseUrl } from '../lib/api-client'
import { formatMoney } from '../lib/format'

export function AnalyticsPage() {
  const performance = useQuery({ queryKey: ['analytics', 'performance'], queryFn: getPerformance })
  const limits = useQuery({ queryKey: ['analytics', 'limits'], queryFn: getRiskLimits })
  const journal = useQuery({ queryKey: ['analytics', 'journal'], queryFn: getJournal })
  const orders = useOrders()
  const client = useQueryClient()
  const saveLimits = useMutation({ mutationFn: saveRiskLimits, onSuccess: () => client.invalidateQueries({ queryKey: ['analytics', 'limits'] }) })
  const saveNote = useMutation({ mutationFn: ({ id, note }: { id: string; note: string }) => saveJournal(id, note), onSuccess: () => client.invalidateQueries({ queryKey: ['analytics', 'journal'] }) })
  const removeNote = useMutation({ mutationFn: deleteJournal, onSuccess: () => client.invalidateQueries({ queryKey: ['analytics', 'journal'] }) })
  const [daily, setDaily] = useState('')
  const [concentration, setConcentration] = useState('')
  const [notes, setNotes] = useState<Record<string, string>>({})
  const [exportError, setExportError] = useState('')

  async function download(kind: string) {
    setExportError('')
    try {
      const response = await fetch(`${apiBaseUrl}/api/analytics/export/${kind}`, { credentials: 'include' })
      if (!response.ok) throw new Error('Export failed')
      const url = URL.createObjectURL(await response.blob())
      const link = document.createElement('a')
      link.href = url
      link.download = `papertrade-${kind}.csv`
      link.click()
      URL.revokeObjectURL(url)
    } catch { setExportError('Could not export the CSV. Try again.') }
  }

  return <main className="min-h-screen px-6 py-8"><div className="mx-auto max-w-6xl">
    <header className="flex flex-col gap-5 sm:flex-row sm:items-center sm:justify-between"><div><Brand /><h1 className="mt-2 text-3xl font-bold text-white">Risk & analytics</h1></div><AppNav /></header>
    <section className="mt-8 rounded-2xl border border-slate-800 bg-slate-900 p-6">
      <h2 className="text-xl font-semibold text-white">Performance history</h2>
      <p className="mt-1 text-sm text-slate-400">Equity is sampled when the portfolio is viewed. Historical values before tracking began use the initial balance.</p>
      {performance.isError && <p role="alert" className="mt-3 text-red-300">Performance could not be loaded.</p>}
      {performance.data && <>
        <div className="mt-5 grid gap-4 sm:grid-cols-2"><Metric label="Maximum drawdown" value={`${performance.data.maxDrawdownPercent.toFixed(2)}%`} /><Metric label="Latest equity" value={formatMoney(performance.data.history.at(-1)?.equity ?? 0)} /></div>
        <EquityChart history={performance.data.history} />
        <DrawdownChart history={performance.data.history} maxDrawdown={performance.data.maxDrawdownPercent} />
        <div className="mt-5 grid gap-4 md:grid-cols-3"><PeriodList title="Daily P&L" items={performance.data.daily} /><PeriodList title="Weekly P&L" items={performance.data.weekly} /><PeriodList title="Monthly P&L" items={performance.data.monthly} /></div>
        <div className="mt-5 overflow-x-auto"><h3 className="mb-2 font-semibold text-white">Recent observations</h3><table className="w-full min-w-[600px] text-left text-sm"><thead className="text-slate-400"><tr><th className="py-2">Time</th><th>Equity</th><th>Realized P&L</th><th>Unrealized P&L</th><th>Drawdown</th></tr></thead><tbody>{performance.data.history.slice(-10).reverse().map(snapshot => <tr key={snapshot.recordedAt} className="border-t border-slate-800 text-slate-300"><td className="py-2">{new Date(snapshot.recordedAt).toLocaleString()}</td><td>{formatMoney(snapshot.equity)}</td><td>{formatMoney(snapshot.realizedPnl)}</td><td>{formatMoney(snapshot.unrealizedPnl)}</td><td>{snapshot.drawdownPercent.toFixed(2)}%</td></tr>)}</tbody></table></div>
      </>}
    </section>
    <section className="mt-6 rounded-2xl border border-slate-800 bg-slate-900 p-6">
      <h2 className="text-xl font-semibold text-white">Portfolio risk limits</h2>
      <p className="mt-1 text-sm text-slate-400">Optional limits block new buys. Sells remain available. Daily loss uses UTC day and sampled equity.</p>
      {limits.data && <form key={`${limits.data.maxDailyLossPercent}-${limits.data.maxPositionConcentrationPercent}`} className="mt-4 grid gap-4 sm:grid-cols-2" onSubmit={event => {
        event.preventDefault()
        saveLimits.mutate({ maxDailyLossPercent: parseLimit(daily || String(limits.data.maxDailyLossPercent ?? '')),
          maxPositionConcentrationPercent: parseLimit(concentration || String(limits.data.maxPositionConcentrationPercent ?? '')) })
      }}>
        <label className="text-sm text-slate-300">Maximum daily loss %<input type="number" min="0.01" max="100" step="0.01" placeholder={String(limits.data.maxDailyLossPercent ?? 'Off')} value={daily} onChange={event => setDaily(event.target.value)} className="mt-1 block w-full rounded border border-slate-700 bg-slate-950 p-2 text-white" /></label>
        <label className="text-sm text-slate-300">Maximum single position %<input type="number" min="0.01" max="100" step="0.01" placeholder={String(limits.data.maxPositionConcentrationPercent ?? 'Off')} value={concentration} onChange={event => setConcentration(event.target.value)} className="mt-1 block w-full rounded border border-slate-700 bg-slate-950 p-2 text-white" /></label>
        <p className="text-sm text-slate-400 sm:col-span-2">Leave a field blank to keep its current value. Enter 0 to switch a limit off.</p>
        <button type="submit" disabled={saveLimits.isPending} className="rounded bg-emerald-400 px-4 py-2 font-semibold text-slate-950 disabled:opacity-50">Save limits</button>
        {saveLimits.isError && <p role="alert" className="text-red-300">Could not save limits.</p>}
      </form>}
    </section>
    <section className="mt-6 rounded-2xl border border-slate-800 bg-slate-900 p-6">
      <h2 className="text-xl font-semibold text-white">Trading journal</h2>
      <p className="mt-1 text-sm text-slate-400">One editable note per order.</p>
      {journal.isError && <p role="alert" className="mt-3 text-red-300">Journal could not be loaded.</p>}
      {orders.data?.length === 0 && <p className="mt-4 text-slate-400">Place an order to start a journal.</p>}
      <div className="mt-4 space-y-4">{orders.data?.map(order => {
        const stored = journal.data?.find(entry => entry.orderId === order.id)?.note ?? ''
        return <article key={order.id} className="rounded-lg border border-slate-700 p-4"><div className="flex flex-wrap items-center justify-between gap-2"><strong className="text-white">{order.symbol} · {order.side} {order.type}</strong><span className="text-xs text-slate-400">{new Date(order.createdAt).toLocaleString()}</span></div>
          <textarea aria-label={`Journal note for ${order.symbol} ${order.id}`} maxLength={4000} value={notes[order.id] ?? stored} onChange={event => setNotes(current => ({ ...current, [order.id]: event.target.value }))} rows={3} placeholder="Why did you take this trade? What did you learn?" className="mt-3 w-full rounded border border-slate-700 bg-slate-950 p-2 text-white" />
          <div className="mt-2 flex gap-2"><button type="button" disabled={saveNote.isPending || !(notes[order.id] ?? stored).trim()} onClick={() => saveNote.mutate({ id: order.id, note: notes[order.id] ?? stored })} className="rounded bg-emerald-400 px-3 py-1 text-sm font-semibold text-slate-950 disabled:opacity-50">Save note</button>{stored && <button type="button" disabled={removeNote.isPending} onClick={() => { removeNote.mutate(order.id); setNotes(current => ({ ...current, [order.id]: '' })) }} className="rounded border border-slate-600 px-3 py-1 text-sm text-slate-300">Delete</button>}</div>
        </article>
      })}</div>
      {(saveNote.isError || removeNote.isError) && <p role="alert" className="mt-3 text-red-300">Journal change failed.</p>}
    </section>
    <section className="my-6 rounded-2xl border border-slate-800 bg-slate-900 p-6"><h2 className="text-xl font-semibold text-white">CSV exports</h2><div className="mt-4 flex flex-wrap gap-2">{['orders', 'executions', 'portfolio', 'journal'].map(kind => <button type="button" key={kind} onClick={() => download(kind)} className="rounded border border-slate-600 px-3 py-2 text-sm capitalize text-slate-200">{kind}</button>)}</div>{exportError && <p role="alert" className="mt-2 text-red-300">{exportError}</p>}</section>
  </div></main>
}

function parseLimit(value: string) { const number = Number(value); return number > 0 ? number : null }
function Metric({ label, value }: { label: string; value: string }) { return <div className="rounded-xl border border-slate-700 p-4"><p className="text-sm text-slate-400">{label}</p><p className="mt-1 text-xl font-semibold text-white">{value}</p></div> }
function PeriodList({ title, items }: { title: string; items: PeriodPnl[] }) { return <div className="rounded-xl border border-slate-700 p-4"><h3 className="font-semibold text-white">{title}</h3><div className="mt-2 max-h-48 overflow-y-auto text-sm">{items.length === 0 ? <p className="text-slate-500">No history yet</p> : items.slice(-12).reverse().map(item => <div key={item.period} className="flex justify-between gap-2 border-t border-slate-800 py-2"><span className="text-slate-400">{item.period}</span><span className={item.pnl >= 0 ? 'text-emerald-400' : 'text-red-400'}>{item.pnl >= 0 ? '+' : ''}{formatMoney(item.pnl)}</span></div>)}</div></div> }
function EquityChart({ history }: { history: Snapshot[] }) {
  if (history.length < 2) return <p className="mt-4 text-sm text-slate-500">The equity curve appears after another observation.</p>
  const values = history.map(x => x.equity)
  const low = Math.min(...values); const high = Math.max(...values); const span = high - low || 1
  const points = history.map((x, i) => `${(i / (history.length - 1) * 100).toFixed(2)},${(95 - (x.equity - low) / span * 90).toFixed(2)}`).join(' ')
  return <div className="mt-4"><svg viewBox="0 0 100 100" preserveAspectRatio="none" role="img" aria-label="Portfolio equity curve" className="h-44 w-full rounded-lg bg-slate-950"><polyline points={points} fill="none" stroke="#34d399" strokeWidth="1.5" vectorEffect="non-scaling-stroke" /></svg><div className="mt-2 flex justify-between text-xs text-slate-500"><span>{new Date(history[0].recordedAt).toLocaleDateString()}</span><span>{new Date(history.at(-1)!.recordedAt).toLocaleDateString()}</span></div></div>
}
function DrawdownChart({ history, maxDrawdown }: { history: Snapshot[]; maxDrawdown: number }) {
  if (history.length < 2) return null
  const span = maxDrawdown || 1
  const points = history.map((x, i) => `${(i / (history.length - 1) * 100).toFixed(2)},${(5 + x.drawdownPercent / span * 90).toFixed(2)}`).join(' ')
  return <div className="mt-5"><h3 className="mb-2 font-semibold text-white">Drawdown history</h3><svg viewBox="0 0 100 100" preserveAspectRatio="none" role="img" aria-label="Portfolio drawdown chart" className="h-32 w-full rounded-lg bg-slate-950"><polyline points={points} fill="none" stroke="#fb7185" strokeWidth="1.5" vectorEffect="non-scaling-stroke" /></svg><p className="mt-1 text-xs text-slate-500">Percentage below the previous equity peak</p></div>
}
