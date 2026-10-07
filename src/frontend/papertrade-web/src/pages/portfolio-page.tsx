import { Link } from 'react-router'
import { AppNav } from '../components/app-nav'
import { Brand } from '../components/brand'
import { usePortfolio } from '../features/trading/trading-queries'
import { ApiError } from '../lib/api-client'
import { formatMoney, formatInstrumentQuantity, formatPrice } from '../lib/format'
import { useRealtimeSymbol } from '../features/realtime/use-realtime-symbol'
import type { Position } from '../features/trading/trading-types'
import { marketDetailPath } from '../features/markets/market-path'
import { MarginPanel } from '../components/margin-panel'

export function PortfolioPage() {
  const portfolio = usePortfolio()
  const error = portfolio.error instanceof ApiError
    ? portfolio.error.message
    : portfolio.isError
      ? 'The portfolio could not be loaded.'
      : null

  return (
    <main className="min-h-screen px-6 py-8">
      <div className="mx-auto max-w-6xl">
        <header className="flex flex-col gap-5 sm:flex-row sm:items-center sm:justify-between">
          <div><Brand /><h1 className="mt-2 text-3xl font-bold text-white">Portfolio</h1></div>
          <AppNav />
        </header>

        {portfolio.isLoading && <p className="mt-10 text-slate-400">Valuing your portfolio...</p>}
        {error && <p role="alert" className="mt-8 rounded-lg border border-red-900 bg-red-950/50 p-3 text-red-300">{error}</p>}

        {portfolio.data && (
          <>
            <section className="mt-10 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
              <Metric label="Portfolio value" value={formatMoney(portfolio.data.portfolioValue)} />
              <Metric label="Available cash" value={formatMoney(portfolio.data.cashBalance)} />
              <Metric label="Unrealized P&L" value={formatSignedMoney(portfolio.data.unrealizedPnl)} tone={portfolio.data.unrealizedPnl} />
              <Metric label="Realized P&L" value={formatSignedMoney(portfolio.data.realizedPnl)} tone={portfolio.data.realizedPnl} />
            </section>

            <section className="mt-4 grid gap-4 sm:grid-cols-2">
              <Metric label="Position equity" value={formatMoney(portfolio.data.marketValue)} />
              <Metric label="Gross exposure" value={formatMoney(portfolio.data.grossExposure)} />
              <Metric label="Total return" value={`${portfolio.data.totalReturnPercentage >= 0 ? '+' : ''}${portfolio.data.totalReturnPercentage.toFixed(2)}%`} tone={portfolio.data.totalReturnPercentage} />
            </section>

            <MarginPanel portfolio={portfolio.data} />

            <section className="mt-8 overflow-hidden rounded-2xl border border-slate-800 bg-slate-900">
              <div className="flex items-center justify-between border-b border-slate-800 p-5"><h2 className="text-xl font-semibold text-white">Positions</h2><Link to="/orders" className="text-sm text-emerald-400 hover:text-emerald-300">View order history</Link></div>
              {portfolio.data.positions.length === 0 ? (
                <div className="p-6"><p className="text-slate-400">No open positions.</p><Link to="/markets" className="mt-3 inline-block text-emerald-400">Browse markets</Link></div>
              ) : (
                <div className="overflow-x-auto"><table className="w-full min-w-[900px] text-left text-sm"><thead className="bg-slate-950/50 text-slate-400"><tr><Th>Symbol</Th><Th>Quantity</Th><Th>Average cost</Th><Th>Price</Th><Th>Exposure</Th><Th>Margin</Th><Th>Position equity</Th><Th>Unrealized P&L</Th></tr></thead><tbody>{portfolio.data.positions.map((position) => <PositionRow key={position.id} position={position} />)}</tbody></table></div>
              )}
            </section>
          </>
        )}
      </div>
    </main>
  )
}

function Metric({ label, value, tone }: { label: string; value: string; tone?: number }) {
  const color = tone === undefined ? 'text-white' : tone >= 0 ? 'text-emerald-400' : 'text-red-400'
  return <article className="rounded-2xl border border-slate-800 bg-slate-900 p-5"><p className="text-sm text-slate-400">{label}</p><p className={`mt-2 text-2xl font-semibold ${color}`}>{value}</p></article>
}
function Th({ children }: { children: React.ReactNode }) { return <th className="px-5 py-3 font-medium">{children}</th> }
function Td({ children }: { children: React.ReactNode }) { return <td className="px-5 py-4 text-slate-300">{children}</td> }
function formatSignedMoney(value: number) { return `${value >= 0 ? '+' : '-'}${formatMoney(Math.abs(value))}` }

function PositionRow({ position }: { position: Position }) {
  useRealtimeSymbol(position.symbol)
  return <tr className="border-t border-slate-800"><Td><Link to={marketDetailPath(position.symbol)} className="font-semibold text-white hover:text-emerald-300">{position.symbol}</Link></Td><Td>{formatInstrumentQuantity(position.quantity, position.instrument)}</Td><Td>{formatPrice(position.averageEntryPrice, position.instrument)}</Td><Td>{formatPrice(position.currentPrice, position.instrument)}</Td><Td>{formatMoney(position.notionalValue)}</Td><Td>{formatMoney(position.marginReserved)}</Td><Td>{formatMoney(position.marketValue)}</Td><Td><span className={position.unrealizedPnl >= 0 ? 'text-emerald-400' : 'text-red-400'}>{formatSignedMoney(position.unrealizedPnl)} ({position.returnPercentage >= 0 ? '+' : ''}{position.returnPercentage.toFixed(2)}%)</span></Td></tr>
}
