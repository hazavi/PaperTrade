import { useMemo, useState, type ReactNode } from 'react'
import { Link, useParams } from 'react-router'
import { ArrowLeft, Bell, BriefcaseBusiness, CandlestickChart, LayoutDashboard, ListOrdered, Search, Star } from 'lucide-react'
import { OrderTicket } from '../components/order-ticket'
import { PriceChart } from '../components/price-chart'
import { useMarketHistory, useMarketQuote } from '../features/markets/market-queries'
import type { Timeframe } from '../features/markets/market-types'
import { createPaperHistory } from '../features/markets/paper-history'
import { useRealtimeSymbol } from '../features/realtime/use-realtime-symbol'
import { ApiError } from '../lib/api-client'

const timeframes: Timeframe[] = ['1D', '1W', '1M', '3M', '1Y']

export function MarketDetailPage() {
  const { symbol: rawSymbol = '' } = useParams()
  const symbol = decodeURIComponent(rawSymbol).toUpperCase()
  const [timeframe, setTimeframe] = useState<Timeframe>('1M')
  const [riskLevels, setRiskLevels] = useState<{ takeProfit: number | null; stopLoss: number | null }>({ takeProfit: null, stopLoss: null })
  useRealtimeSymbol(symbol)
  const quote = useMarketQuote(symbol)
  const history = useMarketHistory(symbol, timeframe)
  const queryError = quote.error
  const error = queryError instanceof ApiError ? queryError.message : queryError ? 'Market data is temporarily unavailable.' : null
  const isPositive = (quote.data?.change ?? 0) >= 0
  const hasLiveHistory = Boolean(history.data?.length)
  const chartPrices = useMemo(() => {
    if (history.data?.length) return history.data
    return quote.data ? createPaperHistory(symbol, timeframe, quote.data) : []
  }, [history.data, quote.data, symbol, timeframe])

  return (
    <main className="terminal-shell">
      <header className="terminal-command-bar">
        <Link to="/markets" className="terminal-icon-button" aria-label="Back to markets"><ArrowLeft /></Link>
        <Link to="/dashboard" className="terminal-logo" aria-label="PaperTrade dashboard">PT</Link>
        <div className="terminal-symbol"><CandlestickChart /><strong>{symbol}</strong><span>US</span></div>
        <div className="terminal-timeframes" role="group" aria-label="Chart timeframe">
          {timeframes.map((value) => <button key={value} type="button" onClick={() => setTimeframe(value)} aria-pressed={timeframe === value} className={timeframe === value ? 'is-active' : ''}>{value}</button>)}
        </div>
        <span className="terminal-command-divider" />
        <nav className="terminal-quick-nav" aria-label="Application navigation">
          <TerminalLink to="/dashboard" label="Dashboard" icon={<LayoutDashboard />} />
          <TerminalLink to="/markets" label="Markets" icon={<Search />} active />
          <TerminalLink to="/portfolio" label="Portfolio" icon={<BriefcaseBusiness />} />
          <TerminalLink to="/orders" label="Orders" icon={<ListOrdered />} />
          <TerminalLink to="/watchlists" label="Watchlists" icon={<Star />} />
          <TerminalLink to="/notifications" label="Notifications" icon={<Bell />} />
        </nav>
      </header>

      {error && <p role="alert" className="terminal-error">{error}</p>}

      <div className="terminal-workspace">
        <section className="terminal-chart-panel">
          <div className="terminal-market-heading">
            <div className="terminal-market-identity">
              <span className="terminal-flag">PT</span>
              <div><strong>{symbol} / U.S. Dollar · {timeframe}</strong><span>PaperTrade Exchange</span></div>
            </div>
            {quote.data && <div className="terminal-ohlc">
              <span>O <b>{quote.data.open.toFixed(2)}</b></span>
              <span>H <b>{quote.data.high.toFixed(2)}</b></span>
              <span>L <b>{quote.data.low.toFixed(2)}</b></span>
              <span>C <b>{quote.data.currentPrice.toFixed(2)}</b></span>
              <em className={isPositive ? 'is-positive' : 'is-negative'}>{isPositive ? '+' : ''}{quote.data.change.toFixed(2)} ({isPositive ? '+' : ''}{quote.data.percentChange.toFixed(2)}%)</em>
            </div>}
            {chartPrices.length > 0 && <span className={`chart-data-badge ${hasLiveHistory ? 'is-live' : 'is-paper'}`}>{hasLiveHistory ? 'Live history' : 'Paper simulation'}</span>}
          </div>

          {history.isLoading && !chartPrices.length
            ? <p className="terminal-chart-loading">Loading price history...</p>
            : chartPrices.length
              ? <PriceChart prices={chartPrices} takeProfit={riskLevels.takeProfit} stopLoss={riskLevels.stopLoss} />
              : <p className="terminal-chart-loading">Waiting for a market quote...</p>}

          <footer className="terminal-range-bar">
            <div>{timeframes.map((value) => <button key={value} type="button" onClick={() => setTimeframe(value)} className={timeframe === value ? 'is-active' : ''}>{value}</button>)}<Link to={`/alerts?symbol=${encodeURIComponent(symbol)}`}>Alert</Link></div>
            <strong>{new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', second: '2-digit' })}</strong>
          </footer>
        </section>

        {quote.data
          ? <OrderTicket symbol={symbol} price={quote.data.currentPrice} onRiskLevelsChange={setRiskLevels} />
          : <aside className="order-panel order-panel--loading">Loading order ticket...</aside>}
      </div>
    </main>
  )
}

function TerminalLink({ to, label, icon, active = false }: { to: string; label: string; icon: ReactNode; active?: boolean }) {
  return <Link to={to} aria-label={label} title={label} className={active ? 'is-active' : ''}>{icon}<span>{label}</span></Link>
}
