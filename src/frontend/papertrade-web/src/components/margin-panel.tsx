import { useState, type FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { getFinancingCharges, saveMarginSettings } from '../features/trading/trading-api'
import { tradingKeys, useMarginSettings } from '../features/trading/trading-queries'
import type { MarginSettings, Portfolio } from '../features/trading/trading-types'
import { formatMoney } from '../lib/format'

export function MarginPanel({ portfolio }: { portfolio: Portfolio }) {
  const settings = useMarginSettings()
  const charges = useQuery({ queryKey: ['trading', 'financing'], queryFn: getFinancingCharges })
  return <section className="mt-8 rounded-2xl border border-slate-800 bg-slate-900 p-5">
    <h2 className="text-xl font-semibold text-white">Leverage and margin simulation</h2>
    <p className="mt-1 text-sm text-slate-400">Paper trades only. Leverage is off by default. Financing accrues on borrowed exposure each UTC day; a margin level below maintenance triggers automatic liquidation.</p>
    <div className="mt-4 grid gap-3 sm:grid-cols-3"><Stat label="Used margin" value={formatMoney(portfolio.usedMargin)} /><Stat label="Available margin" value={formatMoney(portfolio.availableMargin)} /><Stat label="Margin level" value={portfolio.marginLevelPercent === null ? 'No positions' : `${portfolio.marginLevelPercent.toFixed(1)}%`} /></div>
    {portfolio.maintenanceWarning && <p role="alert" className="mt-3 rounded border border-amber-800 bg-amber-950/40 p-3 text-amber-300">Below maintenance margin. A leveraged position may be liquidated at the next worker check.</p>}
    {settings.isError && <p role="alert" className="mt-4 text-red-300">Margin settings could not be loaded.</p>}
    {settings.data && <SettingsForm key={JSON.stringify(settings.data)} initial={settings.data} />}
    <h3 className="mt-5 font-semibold text-white">Financing history</h3>
    {charges.data?.length === 0 && <p className="mt-1 text-sm text-slate-500">No charges.</p>}
    {charges.data?.slice(0, 10).map(charge => <p key={charge.id} className="mt-2 flex justify-between border-t border-slate-800 pt-2 text-sm text-slate-300"><span>{charge.symbol} · {new Date(charge.chargedAt).toLocaleDateString()}</span><span>-{formatMoney(charge.amount)}</span></p>)}
  </section>
}

function Stat({ label, value }: { label: string; value: string }) { return <div className="rounded-lg border border-slate-700 p-3"><p className="text-xs text-slate-400">{label}</p><p className="mt-1 font-semibold text-white">{value}</p></div> }

function SettingsForm({ initial }: { initial: MarginSettings }) {
  const [value, setValue] = useState(initial)
  const client = useQueryClient()
  const save = useMutation({ mutationFn: saveMarginSettings, onSuccess: async () => {
    await Promise.all([client.invalidateQueries({ queryKey: tradingKeys.margin }), client.invalidateQueries({ queryKey: tradingKeys.portfolio })])
  } })
  function submit(event: FormEvent) { event.preventDefault(); save.mutate(value) }
  return <form onSubmit={submit} className="mt-5 space-y-4">
    <label className="flex items-center gap-2 text-sm text-slate-200"><input type="checkbox" checked={value.enabled} onChange={event => setValue(current => ({ ...current, enabled: event.target.checked }))} /> Enable simulated leverage for new buys</label>
    <div className="grid gap-3 sm:grid-cols-3">{([
      ['equityLeverage', 'Equities / ETFs', 5], ['forexLeverage', 'Forex', 10],
      ['metalLeverage', 'Metals', 5], ['commodityLeverage', 'Commodities', 5],
      ['indexLeverage', 'Indices', 5], ['cryptoLeverage', 'Crypto', 3],
    ] as const).map(([key, label, max]) => <label key={key} className="text-sm text-slate-300">{label}<select value={value[key]} onChange={event => setValue(current => ({ ...current, [key]: Number(event.target.value) }))} className="mt-1 block w-full rounded border border-slate-700 bg-slate-950 p-2 text-white">{Array.from({ length: max }, (_, index) => index + 1).map(level => <option key={level} value={level}>{level}×</option>)}</select></label>)}</div>
    <button type="submit" disabled={save.isPending} className="rounded bg-emerald-400 px-4 py-2 font-semibold text-slate-950 disabled:opacity-50">Save margin settings</button>
    {save.isError && <p role="alert" className="text-red-300">Could not save margin settings.</p>}
    {save.isSuccess && <p className="text-emerald-300">Settings saved. Open positions keep their existing margin.</p>}
  </form>
}
