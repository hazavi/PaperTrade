import { useMemo, useState, type ReactNode } from 'react'
import { Link, useParams } from 'react-router'
import { ArrowLeft, Bell, BriefcaseBusiness, CandlestickChart, LayoutDashboard, ListOrdered, Search, Star } from 'lucide-react'
import { OrderTicket } from '../components/order-ticket'
import { Brand } from '../components/brand'
import { PriceChart } from '../components/price-chart'
import { useInstrument, useMarketHistory, useMarketQuote } from '../features/markets/market-queries'
import type { Timeframe } from '../features/markets/market-types'
import { createPaperHistory, mergeLiveQuote } from '../features/markets/paper-history'
import { useRealtimeSymbol } from '../features/realtime/use-realtime-symbol'
import { ApiError } from '../lib/api-client'
import { formatPrice } from '../lib/format'

const timeframes: Timeframe[] = ['1D', '1W', '1M', '3M', '1Y']

export function MarketDetailPage() {
  const { symbol: rawSymbol = '' } = useParams()
  const symbol = decodeURIComponent(rawSymbol).toUpperCase()
  const [timeframe, setTimeframe] = useState<Timeframe>('1M')
  const [demoMode, setDemoMode] = useState(false)
  const [riskLevels, setRiskLevels] = useState<{ takeProfit: number | null; stopLoss: number | null }>({ takeProfit: null, stopLoss: null })
  useRealtimeSymbol(symbol)
  const instrument = useInstrument(symbol)
  const quote = useMarketQuote(symbol)
  const history = useMarketHistory(symbol, timeframe)
  const queryError = quote.error ?? instrument.error
  const error = queryError instanceof ApiError ? queryError.message : queryError ? 'Market data is temporarily unavailable.' : null
  const isPositive = (quote.data?.change ?? 0) >= 0
  const hasLiveHistory = Boolean(history.data?.length)
  const chartPrices = useMemo(() => {
    if (history.data?.length) return history.data
    return demoMode && quote.data ? createPaperHistory(symbol, timeframe, quote.data) : []
  }, [demoMode, history.data, quote.data, symbol, timeframe])
  const displayPrices = useMemo(
    () => quote.data ? mergeLiveQuote(chartPrices, quote.data, timeframe) : chartPrices,
    [chartPrices, quote.data, timeframe],
  )

  return (
    <main className="terminal-shell">
      <header className="terminal-command-bar">
        <Link to="/markets" className="terminal-icon-button" aria-label="Back to markets"><ArrowLeft /></Link>
        <Brand compact className="terminal-logo" />
        <div className="terminal-symbol"><CandlestickChart /><strong>{symbol}</strong><span>{instrument.data?.exchange ?? 'US'}</span></div>
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
              <div><strong>{instrument.data?.displayName ?? symbol} · {timeframe}</strong><span>{instrument.data?.exchange ?? 'US'} · {instrument.data?.quoteCurrency ?? 'USD'}</span></div>
            </div>
            {quote.data && <div className="terminal-ohlc">
              <span>O <b>{formatPrice(quote.data.open, instrument.data)}</b></span>
              <span>H <b>{formatPrice(quote.data.high, instrument.data)}</b></span>
              <span>L <b>{formatPrice(quote.data.low, instrument.data)}</b></span>
              <span>C <b>{formatPrice(quote.data.currentPrice, instrument.data)}</b></span>
              <em className={isPositive ? 'is-positive' : 'is-negative'}>{isPositive ? '+' : ''}{formatPrice(quote.data.change, instrument.data)} ({isPositive ? '+' : ''}{quote.data.percentChange.toFixed(2)}%)</em>
            </div>}
            <span className={`chart-data-badge ${hasLiveHistory ? 'is-live' : demoMode ? 'is-paper' : 'is-offline'}`}>{hasLiveHistory ? 'Real OHLC' : demoMode ? 'Demo candles' : 'History offline'}</span>
          </div>

          <div className="terminal-chart-stack">
            <PriceChart prices={displayPrices} instrument={instrument.data} takeProfit={riskLevels.takeProfit} stopLoss={riskLevels.stopLoss} />
            {!hasLiveHistory && !demoMode && !history.isLoading && <div className="real-history-gate">
              <strong>Real candle history is unavailable</strong>
              <p>Add <code>TWELVE_DATA_API_KEY</code> to <code>.env</code> and rebuild the API. Demo candles are never shown as real data.</p>
              <button type="button" onClick={() => setDemoMode(true)}>Use demo candles</button>
            </div>}
          </div>

          <footer className="terminal-range-bar">
            <div>{timeframes.map((value) => <button key={value} type="button" onClick={() => setTimeframe(value)} className={timeframe === value ? 'is-active' : ''}>{value}</button>)}<Link to={`/alerts?symbol=${encodeURIComponent(symbol)}`}>Alert</Link></div>
            <strong>{new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', second: '2-digit' })}</strong>
          </footer>
        </section>

        {quote.data
          ? <OrderTicket symbol={symbol} instrument={instrument.data} price={quote.data.currentPrice} onRiskLevelsChange={setRiskLevels} />
          : <aside className="order-panel order-panel--loading">Loading order ticket...</aside>}
      </div>
    </main>
  )
}

function TerminalLink({ to, label, icon, active = false }: { to: string; label: string; icon: ReactNode; active?: boolean }) {
  return <Link to={to} aria-label={label} title={label} className={active ? 'is-active' : ''}>{icon}<span>{label}</span></Link>
}
