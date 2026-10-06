import { AppNav } from '../components/app-nav'
import { Brand } from '../components/brand'
import { useOrders } from '../features/trading/trading-queries'
import { ApiError } from '../lib/api-client'
import { formatMoney, formatInstrumentQuantity, formatPrice } from '../lib/format'

export function OrdersPage() {
  const orders = useOrders()
  const error = orders.error instanceof ApiError ? orders.error.message : orders.isError ? 'Order history could not be loaded.' : null
  return (
    <main className="min-h-screen px-6 py-8"><div className="mx-auto max-w-6xl">
      <header className="flex flex-col gap-5 sm:flex-row sm:items-center sm:justify-between"><div><Brand /><h1 className="mt-2 text-3xl font-bold text-white">Order history</h1></div><AppNav /></header>
      {orders.isLoading && <p className="mt-10 text-slate-400">Loading orders...</p>}
      {error && <p role="alert" className="mt-8 rounded-lg border border-red-900 bg-red-950/50 p-3 text-red-300">{error}</p>}
      {orders.data?.length === 0 && <p className="mt-8 rounded-2xl border border-slate-800 bg-slate-900 p-6 text-slate-400">No orders have been placed.</p>}
      {orders.data && orders.data.length > 0 && <section className="mt-8 overflow-x-auto rounded-2xl border border-slate-800 bg-slate-900"><table className="w-full min-w-[800px] text-left text-sm"><thead className="bg-slate-950/50 text-slate-400"><tr>{['Symbol', 'Side', 'Quantity', 'Execution price', 'Total value', 'Status', 'Timestamp'].map((label) => <th key={label} className="px-5 py-3 font-medium">{label}</th>)}</tr></thead><tbody>{orders.data.map((order) => <tr key={order.id} className="border-t border-slate-800"><td className="px-5 py-4 font-semibold text-white">{order.symbol}</td><td className={`px-5 py-4 font-medium capitalize ${order.side === 'buy' ? 'text-emerald-400' : 'text-red-400'}`}>{order.side}</td><td className="px-5 py-4 text-slate-300">{formatInstrumentQuantity(order.quantity, order.instrument)}</td><td className="px-5 py-4 text-slate-300">{order.executedPrice === null ? '—' : formatPrice(order.executedPrice, order.instrument)}</td><td className="px-5 py-4 text-slate-300">{order.totalValue === null ? '—' : formatMoney(order.totalValue)}</td><td className="px-5 py-4 capitalize text-slate-300">{order.status}</td><td className="px-5 py-4 text-slate-400">{new Date(order.executedAt ?? order.createdAt).toLocaleString()}</td></tr>)}</tbody></table></section>}
    </div></main>
  )
}
