import { type FormEvent, useState } from 'react'
import { useCreateOrder, usePortfolio } from '../features/trading/trading-queries'
import type { Order } from '../features/trading/trading-types'
import { ApiError } from '../lib/api-client'
import { formatMoney, formatQuantity } from '../lib/format'

type Side = 'buy' | 'sell'

export function OrderTicket({ symbol, price }: { symbol: string; price: number }) {
  const [side, setSide] = useState<Side | null>(null)
  const [quantity, setQuantity] = useState('')
  const [confirming, setConfirming] = useState(false)
  const [filledOrder, setFilledOrder] = useState<Order | null>(null)
  const portfolio = usePortfolio()
  const create = useCreateOrder()
  const numericQuantity = Number(quantity)
  const estimatedTotal = Number.isFinite(numericQuantity)
    ? numericQuantity * price
    : 0
  const owned = portfolio.data?.positions.find(
    (position) => position.symbol === symbol,
  )?.quantity ?? 0

  function close() {
    setSide(null)
    setQuantity('')
    setConfirming(false)
    setFilledOrder(null)
    create.reset()
  }

  function review(event: FormEvent) {
    event.preventDefault()
    if (numericQuantity > 0) setConfirming(true)
  }

  function submit() {
    if (!side || numericQuantity <= 0) return
    create.mutate(
      { symbol, side, type: 'market', quantity: numericQuantity },
      { onSuccess: (result) => setFilledOrder(result.order) },
    )
  }

  const error = create.error instanceof ApiError
    ? create.error.message
    : create.isError
      ? 'The order could not be submitted.'
      : null

  return (
    <>
      <section className="mt-6 flex flex-wrap items-center gap-3 rounded-2xl border border-slate-800 bg-slate-900 p-5">
        <button type="button" onClick={() => setSide('buy')} className="rounded-lg bg-emerald-400 px-5 py-3 font-semibold text-slate-950">Buy</button>
        <button type="button" onClick={() => setSide('sell')} className="rounded-lg bg-red-400 px-5 py-3 font-semibold text-slate-950">Sell</button>
        <p className="text-sm text-slate-400">Market orders execute using the latest server quote.</p>
      </section>

      {side && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/80 p-4">
          <section role="dialog" aria-modal="true" aria-labelledby="order-title" className="w-full max-w-md rounded-2xl border border-slate-700 bg-slate-900 p-6 shadow-2xl">
            {filledOrder ? (
              <>
                <h2 id="order-title" className="text-2xl font-bold text-white">Order filled</h2>
                <p className="mt-4 text-slate-300">{side === 'buy' ? 'Bought' : 'Sold'} {formatQuantity(filledOrder.quantity)} {symbol} at {formatMoney(filledOrder.executedPrice ?? price)}.</p>
                <button type="button" onClick={close} className="mt-6 w-full rounded-lg bg-emerald-400 px-4 py-3 font-semibold text-slate-950">Done</button>
              </>
            ) : confirming ? (
              <>
                <h2 id="order-title" className="text-2xl font-bold capitalize text-white">Confirm {side} order</h2>
                <dl className="mt-6 space-y-3 text-sm">
                  <OrderDetail label="Symbol" value={symbol} />
                  <OrderDetail label="Quantity" value={formatQuantity(numericQuantity)} />
                  <OrderDetail label="Estimated price" value={formatMoney(price)} />
                  <OrderDetail label="Estimated total" value={formatMoney(estimatedTotal)} />
                </dl>
                <p className="mt-4 text-xs text-slate-500">The final execution price can differ from this estimate.</p>
                {error && <p role="alert" className="mt-4 rounded-lg border border-red-900 bg-red-950/50 p-3 text-sm text-red-300">{error}</p>}
                <div className="mt-6 flex gap-3">
                  <button type="button" onClick={() => setConfirming(false)} disabled={create.isPending} className="flex-1 rounded-lg border border-slate-700 px-4 py-3 text-slate-200">Back</button>
                  <button type="button" onClick={submit} disabled={create.isPending} className="flex-1 rounded-lg bg-emerald-400 px-4 py-3 font-semibold text-slate-950 disabled:opacity-50">{create.isPending ? 'Submitting...' : 'Confirm'}</button>
                </div>
              </>
            ) : (
              <form onSubmit={review}>
                <h2 id="order-title" className="text-2xl font-bold capitalize text-white">{side} {symbol}</h2>
                <div className="mt-5 grid grid-cols-2 gap-3 text-sm">
                  <div className="rounded-lg bg-slate-800 p-3"><p className="text-slate-400">Available cash</p><p className="mt-1 font-semibold text-white">{portfolio.data ? formatMoney(portfolio.data.cashBalance) : 'Loading...'}</p></div>
                  <div className="rounded-lg bg-slate-800 p-3"><p className="text-slate-400">Owned shares</p><p className="mt-1 font-semibold text-white">{formatQuantity(owned)}</p></div>
                </div>
                <label htmlFor="order-quantity" className="mt-5 block text-sm font-medium text-slate-200">Quantity</label>
                <input id="order-quantity" type="number" min="0.000001" max="1000000" step="0.000001" required value={quantity} onChange={(event) => setQuantity(event.target.value)} className="mt-2 w-full rounded-lg border border-slate-700 bg-slate-950 px-4 py-3 text-white outline-none focus:border-emerald-400" />
                <p className="mt-3 text-sm text-slate-400">Estimated value: {formatMoney(estimatedTotal)}</p>
                <div className="mt-6 flex gap-3">
                  <button type="button" onClick={close} className="flex-1 rounded-lg border border-slate-700 px-4 py-3 text-slate-200">Cancel</button>
                  <button type="submit" disabled={numericQuantity <= 0} className="flex-1 rounded-lg bg-emerald-400 px-4 py-3 font-semibold text-slate-950 disabled:opacity-50">Review order</button>
                </div>
              </form>
            )}
          </section>
        </div>
      )}
    </>
  )
}

function OrderDetail({ label, value }: { label: string; value: string }) {
  return <div className="flex justify-between gap-4"><dt className="text-slate-400">{label}</dt><dd className="font-medium text-white">{value}</dd></div>
}
