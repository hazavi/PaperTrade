import { AppNav } from '../components/app-nav'
import { Brand } from '../components/brand'
import { useCancelOrder, useExecutions, useOrders } from '../features/trading/trading-queries'
import type { Execution, Order } from '../features/trading/trading-types'
import type { Instrument } from '../features/markets/market-types'
import { ApiError } from '../lib/api-client'
import { formatMoney, formatInstrumentQuantity, formatPrice } from '../lib/format'

export function OrdersPage() {
  const orders = useOrders()
  const executions = useExecutions()
  const cancel = useCancelOrder()
  const error = orders.error instanceof ApiError ? orders.error.message
    : orders.isError ? 'Orders could not be loaded.' : null

  return <main className="min-h-screen px-6 py-8"><div className="mx-auto max-w-6xl">
    <header className="flex flex-col gap-5 sm:flex-row sm:items-center sm:justify-between"><div><Brand /><h1 className="mt-2 text-3xl font-bold text-white">Orders</h1></div><AppNav /></header>
    {orders.isLoading && <p className="mt-10 text-slate-400">Loading orders...</p>}
    {error && <p role="alert" className="mt-8 rounded-lg border border-red-900 bg-red-950/50 p-3 text-red-300">{error}</p>}
    {cancel.isError && <p role="alert" className="mt-8 text-red-300">Could not cancel the order. Refresh and try again.</p>}
    {orders.data?.length === 0 && <p className="mt-8 rounded-2xl border border-slate-800 bg-slate-900 p-6 text-slate-400">No orders have been placed.</p>}
    {orders.data && orders.data.length > 0 && <section className="mt-8 overflow-x-auto rounded-2xl border border-slate-800 bg-slate-900"><table className="w-full min-w-[900px] text-left text-sm"><thead className="bg-slate-950/50 text-slate-400"><tr>{['Symbol', 'Side / type', 'Filled / quantity', 'Target', 'Execution', 'Status', 'Created', 'Action'].map(label => <th key={label} className="px-4 py-3 font-medium">{label}</th>)}</tr></thead><tbody>{orders.data.map(order => <OrderRow key={order.id} order={order} cancel={() => cancel.mutate(order.id)} cancelling={cancel.isPending} />)}</tbody></table></section>}

    <h2 className="mt-10 text-xl font-semibold text-white">Executions</h2>
    <p className="mt-1 text-sm text-slate-400">Each fill is recorded with its quote, execution price, and fee.</p>
    {executions.isError && <p role="alert" className="mt-4 text-red-300">Executions could not be loaded.</p>}
    {executions.data?.length === 0 && <p className="mt-4 text-slate-500">No executions yet.</p>}
    {executions.data && executions.data.length > 0 && <section className="mt-4 overflow-x-auto rounded-2xl border border-slate-800 bg-slate-900"><table className="w-full min-w-[800px] text-left text-sm"><thead className="bg-slate-950/50 text-slate-400"><tr>{['Time', 'Symbol', 'Side', 'Quantity', 'Quote', 'Fill', 'Value', 'Fee', 'Realized P&L'].map(label => <th key={label} className="px-4 py-3 font-medium">{label}</th>)}</tr></thead><tbody>{executions.data.map(item => <ExecutionRow key={item.id} execution={item} instrument={orders.data?.find(order => order.id === item.orderId)?.instrument} />)}</tbody></table></section>}
  </div></main>
}

function OrderRow({ order, cancel, cancelling }: { order: Order; cancel: () => void; cancelling: boolean }) {
  const isOpen = order.status === 'pending' || order.status === 'partially_filled'
  return <tr className="border-t border-slate-800 text-slate-300"><td className="px-4 py-3 font-semibold text-white">{order.symbol}</td><td className="px-4 py-3 capitalize">{order.side} {order.type}{order.parentOrderId && <span className="ml-1 text-xs text-slate-500">exit</span>}</td><td className="px-4 py-3">{formatInstrumentQuantity(order.filledQuantity, order.instrument)} / {formatInstrumentQuantity(order.quantity, order.instrument)}</td><td className="px-4 py-3">{order.type === 'market' || order.type === 'bracket' ? 'Market' : formatPrice(order.requestedPrice, order.instrument)}</td><td className="px-4 py-3">{order.executedPrice === null ? '—' : formatPrice(order.executedPrice, order.instrument)}</td><td className="px-4 py-3 capitalize">{order.status.replace('_', ' ')}</td><td className="px-4 py-3">{new Date(order.createdAt).toLocaleString()}</td><td className="px-4 py-3">{isOpen && <button type="button" onClick={cancel} disabled={cancelling} className="rounded-lg border border-slate-600 px-3 py-1 text-slate-200 disabled:opacity-50">Cancel</button>}</td></tr>
}

function ExecutionRow({ execution, instrument }: { execution: Execution; instrument?: Instrument }) {
  return <tr className="border-t border-slate-800 text-slate-300"><td className="px-4 py-3">{new Date(execution.executedAt).toLocaleString()}</td><td className="px-4 py-3 font-medium text-white">{execution.symbol}</td><td className="px-4 py-3 capitalize">{execution.side}</td><td className="px-4 py-3">{formatInstrumentQuantity(execution.quantity, instrument)}</td><td className="px-4 py-3">{formatPrice(execution.quotePrice, instrument)}</td><td className="px-4 py-3">{formatPrice(execution.price, instrument)}</td><td className="px-4 py-3">{formatMoney(execution.totalValue)}</td><td className="px-4 py-3">{formatMoney(execution.fee)}</td><td className="px-4 py-3">{formatMoney(execution.realizedPnl)}</td></tr>
}
