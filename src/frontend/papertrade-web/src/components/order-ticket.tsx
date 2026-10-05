import { type FormEvent, useState } from 'react'
import { ChevronDown, Grid2X2, Info, X } from 'lucide-react'
import { useCreateOrder, usePortfolio } from '../features/trading/trading-queries'
import type { Order } from '../features/trading/trading-types'
import { ApiError } from '../lib/api-client'
import { formatMoney, formatQuantity } from '../lib/format'

type Side = 'buy' | 'sell'
type RiskLevels = { takeProfit: number | null; stopLoss: number | null }
type OrderTicketProps = { symbol: string; price: number; onRiskLevelsChange?: (levels: RiskLevels) => void }

export function OrderTicket({ symbol, price, onRiskLevelsChange }: OrderTicketProps) {
  const [side, setSide] = useState<Side>('buy')
  const [quantity, setQuantity] = useState('')
  const [takeProfit, setTakeProfit] = useState('')
  const [stopLoss, setStopLoss] = useState('')
  const [takeProfitEnabled, setTakeProfitEnabled] = useState(false)
  const [stopLossEnabled, setStopLossEnabled] = useState(false)
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

  function updateRisk(nextTakeProfit: string, nextStopLoss: string) {
    setTakeProfit(nextTakeProfit)
    setStopLoss(nextStopLoss)
    onRiskLevelsChange?.({ takeProfit: optionalNumber(nextTakeProfit), stopLoss: optionalNumber(nextStopLoss) })
  }

  function toggleTakeProfit() {
    const enabled = !takeProfitEnabled
    setTakeProfitEnabled(enabled)
    if (!enabled) updateRisk('', stopLoss)
  }

  function toggleStopLoss() {
    const enabled = !stopLossEnabled
    setStopLossEnabled(enabled)
    if (!enabled) updateRisk(takeProfit, '')
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
        <div><p className="order-panel__eyebrow">PaperTrade</p><h2>{symbol}</h2></div>
        <div className="order-panel__window-actions" aria-hidden="true"><Grid2X2 /><X /></div>
      </div>

      <div className="order-mode-tabs" role="tablist" aria-label="Trading panel">
        <button type="button" role="tab" aria-selected="true">Order</button>
        <button type="button" role="tab" aria-selected="false" disabled title="Depth of market is coming later">DOM</button>
      </div>

      <div className="order-quote-buttons" role="group" aria-label="Order side">
        <button type="button" onClick={() => selectSide('sell')} aria-pressed={side === 'sell'} className={side === 'sell' ? 'is-active is-sell' : ''}><span>Sell</span><strong>{price.toFixed(2)}</strong></button>
        <span className="order-spread">MKT</span>
        <button type="button" onClick={() => selectSide('buy')} aria-pressed={side === 'buy'} className={side === 'buy' ? 'is-active is-buy' : ''}><span>Buy</span><strong>{price.toFixed(2)}</strong></button>
      </div>

      <div className="order-type-tabs" role="tablist" aria-label="Order type">
        <button type="button" role="tab" aria-selected="true">Market</button>
        <button type="button" role="tab" aria-selected="false" disabled title="Limit orders are coming later">Limit</button>
        <button type="button" role="tab" aria-selected="false" disabled title="Stop orders are coming later">Stop</button>
      </div>

      <div className="order-panel__market-price"><span>Market price</span><strong>{formatMoney(price)}</strong><Info /></div>

      <form onSubmit={review} className="order-panel__form">
        <label htmlFor="order-quantity">Units</label>
        <div className="order-input-wrap"><input id="order-quantity" aria-label="Quantity" type="number" min="0.000001" max="1000000" step="0.000001" required value={quantity} onChange={(event) => setQuantity(event.target.value)} placeholder="0" /><span>shares</span></div>

        <dl className="order-panel__summary">
          <OrderDetail label="Trade value" value={formatMoney(estimatedTotal)} />
          <OrderDetail label="Available cash" value={portfolio.data ? formatMoney(portfolio.data.cashBalance) : 'Loading...'} />
          <OrderDetail label="Owned" value={`${formatQuantity(owned)} shares`} />
        </dl>

        <div className="order-exits-heading"><strong>Exits</strong><ChevronDown /></div>
        <div className="order-exit-control">
          <div><label htmlFor="take-profit">Take profit, price</label><button type="button" role="switch" aria-checked={takeProfitEnabled} aria-label="Enable take profit" onClick={toggleTakeProfit} className={takeProfitEnabled ? 'neo-switch is-on' : 'neo-switch'}><span /></button></div>
          <div className="risk-input risk-input--profit"><span>TP</span><input id="take-profit" disabled={!takeProfitEnabled} type="number" min="0.000001" step="0.000001" value={takeProfit} onChange={(event) => updateRisk(event.target.value, stopLoss)} placeholder="Take-profit price" /></div>
        </div>
        <div className="order-exit-control">
          <div><label htmlFor="stop-loss">Stop loss, price</label><button type="button" role="switch" aria-checked={stopLossEnabled} aria-label="Enable stop loss" onClick={toggleStopLoss} className={stopLossEnabled ? 'neo-switch is-on' : 'neo-switch'}><span /></button></div>
          <div className="risk-input risk-input--loss"><span>SL</span><input id="stop-loss" disabled={!stopLossEnabled} type="number" min="0.000001" step="0.000001" value={stopLoss} onChange={(event) => updateRisk(takeProfit, event.target.value)} placeholder="Stop-loss price" /></div>
        </div>
        <p className="order-panel__helper">TP and SL are visual planning levels. This order executes at market.</p>
        {riskError && <p role="alert" className="order-panel__validation">{riskError}</p>}

        <button type="submit" aria-label="Review order" disabled={numericQuantity <= 0 || Boolean(riskError)} className={`order-submit order-submit--${side}`}>Start creating {side} order</button>
      </form>

      {confirming && <OrderConfirmation side={side} symbol={symbol} price={price} quantity={numericQuantity} total={estimatedTotal} takeProfit={tp} stopLoss={sl} filledOrder={filledOrder} error={error} pending={create.isPending} onBack={() => setConfirming(false)} onSubmit={submit} onDone={resetTicket} />}
    </aside>
  )
}

function OrderConfirmation({ side, symbol, price, quantity, total, takeProfit, stopLoss, filledOrder, error, pending, onBack, onSubmit, onDone }: { side: Side; symbol: string; price: number; quantity: number; total: number; takeProfit: number | null; stopLoss: number | null; filledOrder: Order | null; error: string | null; pending: boolean; onBack: () => void; onSubmit: () => void; onDone: () => void }) {
  return <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/80 p-4"><section role="dialog" aria-modal="true" aria-labelledby="order-title" className="w-full max-w-md rounded-2xl border border-slate-700 bg-slate-900 p-6 shadow-2xl">
    {filledOrder ? <><h2 id="order-title" className="text-2xl font-bold text-white">Order filled</h2><p className="mt-4 text-slate-300">{side === 'buy' ? 'Bought' : 'Sold'} {formatQuantity(filledOrder.quantity)} {symbol} at {formatMoney(filledOrder.executedPrice ?? price)}.</p><button type="button" onClick={onDone} className="mt-6 w-full rounded-lg bg-emerald-400 px-4 py-3 font-semibold text-slate-950">Done</button></> : <><h2 id="order-title" className="text-2xl font-bold capitalize text-white">Confirm {side} order</h2><dl className="mt-6 space-y-3 text-sm"><OrderDetail label="Symbol" value={symbol} /><OrderDetail label="Quantity" value={formatQuantity(quantity)} /><OrderDetail label="Estimated price" value={formatMoney(price)} /><OrderDetail label="Estimated total" value={formatMoney(total)} />{takeProfit && <OrderDetail label="Take-profit plan" value={formatMoney(takeProfit)} />}{stopLoss && <OrderDetail label="Stop-loss plan" value={formatMoney(stopLoss)} />}</dl>{error && <p role="alert" className="mt-4 rounded-lg border border-red-900 bg-red-950/50 p-3 text-sm text-red-300">{error}</p>}<div className="mt-6 flex gap-3"><button type="button" onClick={onBack} disabled={pending} className="flex-1 rounded-lg border border-slate-700 px-4 py-3 text-slate-200">Back</button><button type="button" onClick={onSubmit} disabled={pending} className="flex-1 rounded-lg bg-emerald-400 px-4 py-3 font-semibold text-slate-950 disabled:opacity-50">{pending ? 'Submitting...' : 'Confirm'}</button></div></>}
  </section></div>
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
