import { type FormEvent, useState } from 'react'
import { ChevronDown, Grid2X2, Info, X } from 'lucide-react'
import { useCreateOrder, usePortfolio } from '../features/trading/trading-queries'
import type { Order } from '../features/trading/trading-types'
import type { Instrument, MarketQuote } from '../features/markets/market-types'
import { ApiError } from '../lib/api-client'
import { formatMoney, formatPrice, formatInstrumentQuantity } from '../lib/format'

type Side = 'buy' | 'sell'
type RiskLevels = { takeProfit: number | null; stopLoss: number | null }
type OrderTicketProps = { symbol: string; quote: MarketQuote; instrument?: Instrument; onRiskLevelsChange?: (levels: RiskLevels) => void }

export function OrderTicket({ symbol, quote, instrument, onRiskLevelsChange }: OrderTicketProps) {
  const price = quote.currentPrice
  const [side, setSide] = useState<Side>('buy')
  const [sizeMode, setSizeMode] = useState<'units' | 'lots'>('units')
  const [quantity, setQuantity] = useState('')
  const [takeProfit, setTakeProfit] = useState('')
  const [stopLoss, setStopLoss] = useState('')
  const [takeProfitEnabled, setTakeProfitEnabled] = useState(false)
  const [stopLossEnabled, setStopLossEnabled] = useState(false)
  const [confirming, setConfirming] = useState(false)
  const [filledOrder, setFilledOrder] = useState<Order | null>(null)
  const portfolio = usePortfolio()
  const create = useCreateOrder()
  const enteredQuantity = Number(quantity)
  const numericQuantity = enteredQuantity * (sizeMode === 'lots' ? instrument?.lotSize ?? 1 : 1)
  const estimatedTotal = Number.isFinite(numericQuantity)
    ? instrument?.assetClass === 'forex' && instrument.baseCurrency === 'USD'
      ? numericQuantity
      : numericQuantity * price
    : 0
  const unitLabel = instrument?.assetClass === 'forex' ? 'currency units' : instrument?.assetClass === 'metal' ? 'troy oz' : 'shares'
  const tp = optionalNumber(takeProfit)
  const sl = optionalNumber(stopLoss)
  const owned = portfolio.data?.positions.find((position) => position.instrumentId === instrument?.id || position.symbol === symbol)?.quantity ?? 0
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
    if (numericQuantity >= (instrument?.minimumOrderSize ?? 0.000001) && numericQuantity <= 1_000_000 && numericQuantity % (instrument?.minimumOrderSize ?? 0.000001) < 0.0000001 && !riskError) setConfirming(true)
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
        <button type="button" onClick={() => selectSide('sell')} aria-pressed={side === 'sell'} className={side === 'sell' ? 'is-active is-sell' : ''}><span>Sell</span><strong>{formatPrice(quote.bid ?? price, instrument)}</strong></button>
        <span className="order-spread">{quote.spread && instrument?.pipSize ? `${(quote.spread / instrument.pipSize).toFixed(1)} pip` : 'MKT'}</span>
        <button type="button" onClick={() => selectSide('buy')} aria-pressed={side === 'buy'} className={side === 'buy' ? 'is-active is-buy' : ''}><span>Buy</span><strong>{formatPrice(quote.ask ?? price, instrument)}</strong></button>
      </div>

      <div className="order-type-tabs" role="tablist" aria-label="Order type">
        <button type="button" role="tab" aria-selected="true">Market</button>
        <button type="button" role="tab" aria-selected="false" disabled title="Limit orders are coming later">Limit</button>
        <button type="button" role="tab" aria-selected="false" disabled title="Stop orders are coming later">Stop</button>
      </div>

      <div className="order-panel__market-price"><span>Market price</span><strong>{formatPrice(price, instrument)}</strong><Info /></div>
      {quote.spreadIsSimulated && <p className="order-panel__helper">Bid and ask show an estimated paper spread. Market orders currently fill at the midpoint.</p>}

      <form onSubmit={review} className="order-panel__form">
        <label htmlFor="order-quantity">{sizeMode === 'lots' ? 'Lots' : 'Units'}</label>
        {(instrument?.assetClass === 'forex' || instrument?.assetClass === 'metal') && <div className="flex gap-2 text-sm"><button type="button" onClick={() => { setSizeMode('units'); setQuantity('') }} aria-pressed={sizeMode === 'units'}>Units</button><button type="button" onClick={() => { setSizeMode('lots'); setQuantity('') }} aria-pressed={sizeMode === 'lots'}>Lots</button><span>1 lot = {instrument.lotSize.toLocaleString()} {unitLabel}</span></div>}
        <div className="order-input-wrap"><input id="order-quantity" aria-label="Quantity" type="number" min={(instrument?.minimumOrderSize ?? 0.000001) / (sizeMode === 'lots' ? instrument?.lotSize ?? 1 : 1)} max="1000000" step={(instrument?.minimumOrderSize ?? 0.000001) / (sizeMode === 'lots' ? instrument?.lotSize ?? 1 : 1)} required value={quantity} onChange={(event) => setQuantity(event.target.value)} placeholder="0" /><span>{sizeMode === 'lots' ? 'lots' : unitLabel}</span></div>

        <dl className="order-panel__summary">
          <OrderDetail label="Trade value" value={formatMoney(estimatedTotal)} />
          <OrderDetail label="Available cash" value={portfolio.data ? formatMoney(portfolio.data.cashBalance) : 'Loading...'} />
          <OrderDetail label="Owned" value={`${formatInstrumentQuantity(owned, instrument)} ${unitLabel}`} />
          {instrument?.assetClass === 'forex' && <OrderDetail label="Pip value" value={formatMoney(numericQuantity * instrument.pipSize / (instrument.quoteCurrency === 'USD' ? 1 : price))} />}
        </dl>

        <div className="order-exits-heading"><strong>Exits</strong><ChevronDown /></div>
        <div className="order-exit-control">
          <div><label htmlFor="take-profit">Take profit, price</label><button type="button" role="switch" aria-checked={takeProfitEnabled} aria-label="Enable take profit" onClick={toggleTakeProfit} className={takeProfitEnabled ? 'neo-switch is-on' : 'neo-switch'}><span /></button></div>
          <div className="risk-input risk-input--profit"><span>TP</span><input id="take-profit" disabled={!takeProfitEnabled} type="number" min={instrument?.tickSize ?? 0.01} step={instrument?.tickSize ?? 0.01} value={takeProfit} onChange={(event) => updateRisk(event.target.value, stopLoss)} placeholder="Take-profit price" /></div>
        </div>
        <div className="order-exit-control">
          <div><label htmlFor="stop-loss">Stop loss, price</label><button type="button" role="switch" aria-checked={stopLossEnabled} aria-label="Enable stop loss" onClick={toggleStopLoss} className={stopLossEnabled ? 'neo-switch is-on' : 'neo-switch'}><span /></button></div>
          <div className="risk-input risk-input--loss"><span>SL</span><input id="stop-loss" disabled={!stopLossEnabled} type="number" min={instrument?.tickSize ?? 0.01} step={instrument?.tickSize ?? 0.01} value={stopLoss} onChange={(event) => updateRisk(takeProfit, event.target.value)} placeholder="Stop-loss price" /></div>
        </div>
        <p className="order-panel__helper">TP and SL are visual planning levels. This order executes at market.</p>
        {riskError && <p role="alert" className="order-panel__validation">{riskError}</p>}

        <button type="submit" aria-label="Review order" disabled={numericQuantity < (instrument?.minimumOrderSize ?? 0.000001) || numericQuantity > 1_000_000 || numericQuantity % (instrument?.minimumOrderSize ?? 0.000001) >= 0.0000001 || Boolean(riskError) || !instrument?.isTradable} className={`order-submit order-submit--${side}`}>Start creating {side} order</button>
      </form>

      {confirming && <OrderConfirmation side={side} symbol={symbol} instrument={instrument} price={price} quantity={numericQuantity} total={estimatedTotal} takeProfit={tp} stopLoss={sl} filledOrder={filledOrder} error={error} pending={create.isPending} onBack={() => setConfirming(false)} onSubmit={submit} onDone={resetTicket} />}
    </aside>
  )
}

function OrderConfirmation({ side, symbol, instrument, price, quantity, total, takeProfit, stopLoss, filledOrder, error, pending, onBack, onSubmit, onDone }: { side: Side; symbol: string; instrument?: Instrument; price: number; quantity: number; total: number; takeProfit: number | null; stopLoss: number | null; filledOrder: Order | null; error: string | null; pending: boolean; onBack: () => void; onSubmit: () => void; onDone: () => void }) {
  return <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/80 p-4"><section role="dialog" aria-modal="true" aria-labelledby="order-title" className="w-full max-w-md rounded-2xl border border-slate-700 bg-slate-900 p-6 shadow-2xl">
    {filledOrder ? <><h2 id="order-title" className="text-2xl font-bold text-white">Order filled</h2><p className="mt-4 text-slate-300">{side === 'buy' ? 'Bought' : 'Sold'} {formatInstrumentQuantity(filledOrder.quantity, instrument)} {symbol} at {formatPrice(filledOrder.executedPrice ?? price, instrument)}.</p><button type="button" onClick={onDone} className="mt-6 w-full rounded-lg bg-emerald-400 px-4 py-3 font-semibold text-slate-950">Done</button></> : <><h2 id="order-title" className="text-2xl font-bold capitalize text-white">Confirm {side} order</h2><dl className="mt-6 space-y-3 text-sm"><OrderDetail label="Symbol" value={symbol} /><OrderDetail label="Quantity" value={formatInstrumentQuantity(quantity, instrument)} /><OrderDetail label="Estimated price" value={formatPrice(price, instrument)} /><OrderDetail label="Estimated total" value={formatMoney(total)} />{takeProfit && <OrderDetail label="Take-profit plan" value={formatPrice(takeProfit, instrument)} />}{stopLoss && <OrderDetail label="Stop-loss plan" value={formatPrice(stopLoss, instrument)} />}</dl>{error && <p role="alert" className="mt-4 rounded-lg border border-red-900 bg-red-950/50 p-3 text-sm text-red-300">{error}</p>}<div className="mt-6 flex gap-3"><button type="button" onClick={onBack} disabled={pending} className="flex-1 rounded-lg border border-slate-700 px-4 py-3 font-semibold text-slate-200">Back</button><button type="button" onClick={onSubmit} disabled={pending} className="flex-1 rounded-lg bg-emerald-400 px-4 py-3 font-semibold text-slate-950 disabled:opacity-50">{pending ? 'Submitting...' : 'Confirm'}</button></div></>}
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
