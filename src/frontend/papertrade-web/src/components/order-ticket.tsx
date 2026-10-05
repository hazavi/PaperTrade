import { type FormEvent, useState } from 'react'
import { useCreateOrder, usePortfolio } from '../features/trading/trading-queries'
import type { Order } from '../features/trading/trading-types'
import { ApiError } from '../lib/api-client'
import { formatMoney, formatQuantity } from '../lib/format'

type Side = 'buy' | 'sell'

type OrderTicketProps = {
  symbol: string
  price: number
  onRiskLevelsChange?: (levels: { takeProfit: number | null; stopLoss: number | null }) => void
}

export function OrderTicket({ symbol, price, onRiskLevelsChange }: OrderTicketProps) {
  const [side, setSide] = useState<Side>('buy')
  const [quantity, setQuantity] = useState('')
  const [takeProfit, setTakeProfit] = useState('')
  const [stopLoss, setStopLoss] = useState('')
  const [confirming, setConfirming] = useState(false)
  const [filledOrder, setFilledOrder] = useState<Order | null>(null)
  const portfolio = usePortfolio()
  const create = useCreateOrder()
  const numericQuantity = Number(quantity)
  const estimatedTotal = Number.isFinite(numericQuantity) ? numericQuantity * price : 0
  const tp = optionalNumber(takeProfit)
  const sl = optionalNumber(stopLoss)
  const owned = portfolio.data?.positions.find((position) => position.symbol === symbol)?.quantity ?? 0
  const riskError = validateRiskLevels(side, price, tp, sl)

  function resetTicket() {
    setQuantity('')
    setConfirming(false)
    setFilledOrder(null)
    create.reset()
  }

  function selectSide(value: Side) {
    setSide(value)
    setConfirming(false)
    setFilledOrder(null)
    create.reset()
  }

  function updateTakeProfit(value: string) {
    setTakeProfit(value)
    onRiskLevelsChange?.({ takeProfit: optionalNumber(value), stopLoss: sl })
  }

  function updateStopLoss(value: string) {
    setStopLoss(value)
    onRiskLevelsChange?.({ takeProfit: tp, stopLoss: optionalNumber(value) })
  }

  function review(event: FormEvent) {
    event.preventDefault()
    if (numericQuantity > 0 && !riskError) setConfirming(true)
  }

  function submit() {
    if (numericQuantity <= 0) return
    create.mutate(
      { symbol, side, type: 'market', quantity: numericQuantity },
      { onSuccess: (result) => setFilledOrder(result.order) },
    )
  }

  const error = create.error instanceof ApiError ? create.error.message : create.isError ? 'The order could not be submitted.' : null

  return (
    <aside className="order-panel" aria-label="Order ticket">
      <div className="order-panel__heading">
        <div><p className="order-panel__eyebrow">Order ticket</p><h2>{symbol}</h2></div>
        <div className="order-panel__price"><span>Market</span><strong>{formatMoney(price)}</strong></div>
      </div>

      <div className="order-side-switch" role="group" aria-label="Order side">
        <button type="button" onClick={() => selectSide('buy')} aria-pressed={side === 'buy'} className={side === 'buy' ? 'is-buy-active' : ''}>Buy</button>
        <button type="button" onClick={() => selectSide('sell')} aria-pressed={side === 'sell'} className={side === 'sell' ? 'is-sell-active' : ''}>Sell</button>
      </div>

      <form onSubmit={review} className="order-panel__form">
        <label htmlFor="order-quantity">Quantity</label>
        <div className="order-input-wrap"><input id="order-quantity" type="number" min="0.000001" max="1000000" step="0.000001" required value={quantity} onChange={(event) => setQuantity(event.target.value)} placeholder="0" /><span>shares</span></div>

        <div className="order-panel__risk-grid">
          <div><label htmlFor="take-profit">Take profit</label><div className="risk-input risk-input--profit"><span>TP</span><input id="take-profit" aria-describedby="risk-helper" type="number" min="0.000001" step="0.000001" value={takeProfit} onChange={(event) => updateTakeProfit(event.target.value)} placeholder="Optional" /></div></div>
          <div><label htmlFor="stop-loss">Stop loss</label><div className="risk-input risk-input--loss"><span>SL</span><input id="stop-loss" aria-describedby="risk-helper" type="number" min="0.000001" step="0.000001" value={stopLoss} onChange={(event) => updateStopLoss(event.target.value)} placeholder="Optional" /></div></div>
        </div>
        <p id="risk-helper" className="order-panel__helper">TP and SL are visual planning levels. This order executes at market.</p>
        {riskError && <p role="alert" className="order-panel__validation">{riskError}</p>}

        <dl className="order-panel__summary">
          <OrderDetail label="Available cash" value={portfolio.data ? formatMoney(portfolio.data.cashBalance) : 'Loading...'} />
          <OrderDetail label="Owned" value={`${formatQuantity(owned)} shares`} />
          <OrderDetail label="Estimated value" value={formatMoney(estimatedTotal)} />
        </dl>

        <button type="submit" aria-label="Review order" disabled={numericQuantity <= 0 || Boolean(riskError)} className={`order-submit order-submit--${side}`}>Review {side} order</button>
      </form>

      {confirming && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/80 p-4">
          <section role="dialog" aria-modal="true" aria-labelledby="order-title" className="w-full max-w-md rounded-2xl border border-slate-700 bg-slate-900 p-6 shadow-2xl">
            {filledOrder ? (
              <>
                <h2 id="order-title" className="text-2xl font-bold text-white">Order filled</h2>
                <p className="mt-4 text-slate-300">{side === 'buy' ? 'Bought' : 'Sold'} {formatQuantity(filledOrder.quantity)} {symbol} at {formatMoney(filledOrder.executedPrice ?? price)}.</p>
                {(tp || sl) && <p className="mt-3 text-sm text-slate-400">Your TP/SL lines remain on the chart as a trade plan; they are not automatic exit orders.</p>}
                <button type="button" onClick={resetTicket} className="mt-6 w-full rounded-lg bg-emerald-400 px-4 py-3 font-semibold text-slate-950">Done</button>
              </>
            ) : (
              <>
                <h2 id="order-title" className="text-2xl font-bold capitalize text-white">Confirm {side} order</h2>
                <dl className="mt-6 space-y-3 text-sm">
                  <OrderDetail label="Symbol" value={symbol} />
                  <OrderDetail label="Quantity" value={formatQuantity(numericQuantity)} />
                  <OrderDetail label="Estimated price" value={formatMoney(price)} />
                  <OrderDetail label="Estimated total" value={formatMoney(estimatedTotal)} />
                  {tp && <OrderDetail label="Take-profit plan" value={formatMoney(tp)} />}
                  {sl && <OrderDetail label="Stop-loss plan" value={formatMoney(sl)} />}
                </dl>
                <p className="mt-4 text-xs text-slate-500">The final execution price can differ from this estimate.</p>
                {error && <p role="alert" className="mt-4 rounded-lg border border-red-900 bg-red-950/50 p-3 text-sm text-red-300">{error}</p>}
                <div className="mt-6 flex gap-3">
                  <button type="button" onClick={() => setConfirming(false)} disabled={create.isPending} className="flex-1 rounded-lg border border-slate-700 px-4 py-3 text-slate-200">Back</button>
                  <button type="button" onClick={submit} disabled={create.isPending} className="flex-1 rounded-lg bg-emerald-400 px-4 py-3 font-semibold text-slate-950 disabled:opacity-50">{create.isPending ? 'Submitting...' : 'Confirm'}</button>
                </div>
              </>
            )}
          </section>
        </div>
      )}
    </aside>
  )
}

function OrderDetail({ label, value }: { label: string; value: string }) {
  return <div className="flex justify-between gap-4"><dt className="text-slate-400">{label}</dt><dd className="font-medium text-white">{value}</dd></div>
}

function optionalNumber(value: string) {
  if (!value.trim()) return null
  const number = Number(value)
  return Number.isFinite(number) && number > 0 ? number : null
}

function validateRiskLevels(side: Side, price: number, takeProfit: number | null, stopLoss: number | null) {
  if (side === 'buy' && takeProfit !== null && takeProfit <= price) return 'Buy take profit must be above the current price.'
  if (side === 'buy' && stopLoss !== null && stopLoss >= price) return 'Buy stop loss must be below the current price.'
  if (side === 'sell' && takeProfit !== null && takeProfit >= price) return 'Sell take profit must be below the current price.'
  if (side === 'sell' && stopLoss !== null && stopLoss <= price) return 'Sell stop loss must be above the current price.'
  return null
}
