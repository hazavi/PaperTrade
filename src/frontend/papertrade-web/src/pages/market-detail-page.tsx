import { useCallback, useMemo, useState, type ReactNode } from 'react'
import { Link, useNavigate, useParams } from 'react-router'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import type { IRange, Time } from 'lightweight-charts'
import { ArrowLeft, Bell, BriefcaseBusiness, CandlestickChart, LayoutDashboard, ListOrdered, Search, Star } from 'lucide-react'
import { OrderTicket } from '../components/order-ticket'
import { Brand } from '../components/brand'
import { PriceChart, type ChartLayoutState } from '../components/price-chart'
import { createChartLayout, deleteChartLayout, getChartLayouts, updateChartLayout } from '../features/markets/chart-layouts'
import { useInstrument, useMarketHistory, useMarketQuote } from '../features/markets/market-queries'
import type { Timeframe } from '../features/markets/market-types'
import { createPaperHistory, mergeLiveQuote } from '../features/markets/paper-history'
import { useRealtimeSymbol } from '../features/realtime/use-realtime-symbol'
import { ApiError } from '../lib/api-client'
import { formatPrice } from '../lib/format'

const timeframes: Timeframe[] = ['1D', '1W', '1M', '3M', '1Y']

export function MarketDetailPage() {
  const { symbol: rawSymbol = '', baseCurrency, quoteCurrency } = useParams()
  const symbol = (baseCurrency && quoteCurrency ? `${baseCurrency}/${quoteCurrency}` : rawSymbol).toUpperCase()
  const [timeframe, setTimeframe] = useState<Timeframe>('1M')
  const [demoMode, setDemoMode] = useState(false)
  const [riskLevels, setRiskLevels] = useState<{ takeProfit: number | null; stopLoss: number | null }>({ takeProfit: null, stopLoss: null })
  const [layoutState, setLayoutState] = useState<ChartLayoutState>({ style: 'candles', indicators: ['volume'], drawings: [], drawingsVisible: true })
  const [selectedLayout, setSelectedLayout] = useState('')
  const [layoutRevision, setLayoutRevision] = useState(0)
  const [secondSymbol, setSecondSymbol] = useState('')
  const [sharedRange, setSharedRange] = useState<IRange<Time> | null>(null)
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const layouts = useQuery({ queryKey: ['chart-layouts'], queryFn: getChartLayouts })
  const saveLayout = useMutation({ mutationFn: async ({ id, name }: { id?: string; name: string }) => {
    const value = { name, symbol, timeframe, state: { ...layoutState, secondarySymbol: secondSymbol || undefined } }
    return id ? updateChartLayout(id, value) : createChartLayout(value)
  }, onSuccess: (saved) => { setSelectedLayout(saved.id); queryClient.invalidateQueries({ queryKey: ['chart-layouts'] }) } })
  const removeLayout = useMutation({ mutationFn: deleteChartLayout, onSuccess: () => { setSelectedLayout(''); queryClient.invalidateQueries({ queryKey: ['chart-layouts'] }) } })
  const secondInstrument = useInstrument(secondSymbol)
  const secondHistory = useMarketHistory(secondSymbol, timeframe)
  const onRangeChange = useCallback((range: IRange<Time> | null) => setSharedRange(current => JSON.stringify(current) === JSON.stringify(range) ? current : range), [])
  useRealtimeSymbol(symbol)
  const instrument = useInstrument(symbol)
  const quote = useMarketQuote(symbol)
  const history = useMarketHistory(symbol, timeframe)
  const queryError = quote.error ?? instrument.error
  const error = queryError instanceof ApiError ? queryError.message : queryError ? 'Market data is temporarily unavailable.' : null
  const isPositive = (quote.data?.change ?? 0) >= 0
  const hasLiveHistory = Boolean(history.data?.length)
  const isGeneratedHistory = Boolean(history.data?.[0]?.isSimulated)
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
            <span className={`chart-data-badge ${hasLiveHistory && !isGeneratedHistory ? 'is-live' : demoMode || isGeneratedHistory ? 'is-paper' : 'is-offline'}`}>{isGeneratedHistory ? 'Generated index candles' : hasLiveHistory ? `${history.data?.[0]?.source ?? 'Provider'} OHLC` : demoMode ? 'Demo candles' : 'History offline'}</span>
          </div>

          {quote.data && <p className="px-4 py-1 text-xs text-slate-400">{quote.data.isSimulated ? 'Generated paper quote' : quote.data.source ?? 'Market data'} · Updated {new Date(quote.data.timestamp).toLocaleString()}</p>}

          <div className="flex flex-wrap items-center gap-2 px-4 py-2 text-sm text-slate-300">
            <select aria-label="Saved chart layout" value={selectedLayout} onChange={event => {
              const item = layouts.data?.find(x => x.id === event.target.value)
              setSelectedLayout(event.target.value)
              if (!item) return
              if (item.symbol !== symbol) navigate(item.symbol.includes('/') ? `/markets/pair/${item.symbol}` : `/markets/${item.symbol}`)
              setTimeframe(item.timeframe)
              setLayoutState(item.state)
              setSecondSymbol(item.state.secondarySymbol ?? '')
              setLayoutRevision(value => value + 1)
            }} className="rounded border border-slate-700 bg-slate-950 px-2 py-1"><option value="">Current chart</option>{layouts.data?.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}</select>
            <button type="button" onClick={() => { const name = window.prompt('Layout name', layouts.data?.find(x => x.id === selectedLayout)?.name ?? `${symbol} chart`)?.trim(); if (name) saveLayout.mutate({ id: selectedLayout || undefined, name }) }} className="rounded bg-slate-700 px-3 py-1">Save layout</button>
            {selectedLayout && <button type="button" onClick={() => removeLayout.mutate(selectedLayout)} className="rounded border border-slate-700 px-3 py-1">Delete</button>}
            <input aria-label="Second chart symbol" value={secondSymbol} onChange={event => setSecondSymbol(event.target.value.toUpperCase())} placeholder="Second symbol" className="w-36 rounded border border-slate-700 bg-slate-950 px-2 py-1 uppercase" />
            <span>Charts share timeframe and visible dates</span>
            {(saveLayout.error || removeLayout.error) && <span role="alert" className="text-red-300">Could not save chart layout.</span>}
          </div>
          <div className="terminal-chart-stack">
            <PriceChart key={`${symbol}-${layoutRevision}`} prices={displayPrices} instrument={instrument.data} takeProfit={riskLevels.takeProfit} stopLoss={riskLevels.stopLoss} initialLayout={layoutState} onLayoutChange={setLayoutState} visibleRange={sharedRange} onVisibleRangeChange={onRangeChange} />
            {!hasLiveHistory && !demoMode && !history.isLoading && <div className="real-history-gate">
              <strong>Real candle history is unavailable</strong>
              <p>Add <code>TWELVE_DATA_API_KEY</code> to <code>.env</code> and rebuild the API. Demo candles are never shown as real data.</p>
              <button type="button" onClick={() => setDemoMode(true)}>Use demo candles</button>
            </div>}
          </div>
          {secondSymbol && <div className="terminal-chart-stack mt-3"><div className="px-4 py-2 text-sm text-slate-300">{secondSymbol} · {timeframe}</div>{secondHistory.data?.length ? <PriceChart key={`second-${secondSymbol}-${layoutRevision}`} prices={secondHistory.data} instrument={secondInstrument.data} visibleRange={sharedRange} onVisibleRangeChange={onRangeChange} /> : <p className="p-5 text-slate-400">{secondHistory.isLoading ? 'Loading second chart...' : 'No history for this symbol.'}</p>}</div>}

          <footer className="terminal-range-bar">
            <div>{timeframes.map((value) => <button key={value} type="button" onClick={() => setTimeframe(value)} className={timeframe === value ? 'is-active' : ''}>{value}</button>)}<Link to={`/alerts?symbol=${encodeURIComponent(symbol)}`}>Alert</Link></div>
            <strong>{new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', second: '2-digit' })}</strong>
          </footer>
        </section>

        {quote.data
          ? <OrderTicket key={symbol} symbol={symbol} instrument={instrument.data} quote={quote.data} onRiskLevelsChange={setRiskLevels} />
          : <aside className="order-panel order-panel--loading">Loading order ticket...</aside>}
      </div>
    </main>
  )
}

function TerminalLink({ to, label, icon, active = false }: { to: string; label: string; icon: ReactNode; active?: boolean }) {
  return <Link to={to} aria-label={label} title={label} className={active ? 'is-active' : ''}>{icon}<span>{label}</span></Link>
}
