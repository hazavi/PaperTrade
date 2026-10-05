import {
  AreaSeries,
  CandlestickSeries,
  ColorType,
  CrosshairMode,
  LineStyle,
  createChart,
  type IChartApi,
  type IPriceLine,
  type ISeriesApi,
  type UTCTimestamp,
} from 'lightweight-charts'
import { useEffect, useRef, useState } from 'react'
import type { HistoricalPrice } from '../features/markets/market-types'

type ChartStyle = 'candles' | 'area'
type DrawingTool = 'cursor' | 'line' | 'take-profit' | 'stop-loss'

type PriceChartProps = {
  prices: HistoricalPrice[]
  takeProfit?: number | null
  stopLoss?: number | null
}

const toolLabels: Record<DrawingTool, string> = {
  cursor: 'Crosshair',
  line: 'Horizontal line',
  'take-profit': 'Take profit',
  'stop-loss': 'Stop loss',
}

export function PriceChart({ prices, takeProfit, stopLoss }: PriceChartProps) {
  const containerRef = useRef<HTMLDivElement>(null)
  const chartRef = useRef<IChartApi | null>(null)
  const seriesRef = useRef<ISeriesApi<'Candlestick'> | ISeriesApi<'Area'> | null>(null)
  const drawingLinesRef = useRef<IPriceLine[]>([])
  const riskLinesRef = useRef<IPriceLine[]>([])
  const [style, setStyle] = useState<ChartStyle>('candles')
  const [tool, setTool] = useState<DrawingTool>('cursor')
  const [drawingCount, setDrawingCount] = useState(0)

  useEffect(() => {
    const container = containerRef.current
    if (!container) return

    const chart: IChartApi = createChart(container, {
      height: 540,
      layout: {
        background: { type: ColorType.Solid, color: 'transparent' },
        textColor: '#9fb3ca',
        attributionLogo: false,
      },
      grid: {
        vertLines: { color: 'rgba(255, 255, 255, 0.055)', style: LineStyle.Dotted },
        horzLines: { color: 'rgba(255, 255, 255, 0.055)', style: LineStyle.Dotted },
      },
      crosshair: {
        mode: CrosshairMode.Normal,
        vertLine: { color: 'rgba(180, 215, 255, .48)', style: LineStyle.Dashed, labelBackgroundColor: '#263b57' },
        horzLine: { color: 'rgba(180, 215, 255, .48)', style: LineStyle.Dashed, labelBackgroundColor: '#263b57' },
      },
      rightPriceScale: {
        borderColor: 'rgba(255, 255, 255, 0.14)',
        scaleMargins: { top: 0.1, bottom: 0.12 },
      },
      timeScale: {
        borderColor: 'rgba(255, 255, 255, 0.14)',
        timeVisible: true,
        secondsVisible: false,
        rightOffset: 5,
      },
      handleScroll: true,
      handleScale: true,
    })

    const series = style === 'candles'
      ? chart.addSeries(CandlestickSeries, {
          upColor: '#18c7a1',
          downColor: '#ff5f74',
          borderVisible: false,
          wickUpColor: '#18c7a1',
          wickDownColor: '#ff5f74',
          priceLineColor: '#7ddcff',
          priceLineStyle: LineStyle.Dotted,
        })
      : chart.addSeries(AreaSeries, {
          lineColor: '#7ddcff',
          lineWidth: 2,
          topColor: 'rgba(125, 220, 255, 0.32)',
          bottomColor: 'rgba(128, 143, 255, 0.015)',
          priceLineColor: '#7ddcff',
          priceLineStyle: LineStyle.Dotted,
        })

    if (style === 'candles') {
      ;(series as ISeriesApi<'Candlestick'>).setData(prices.map((price) => ({
        time: toTimestamp(price.time),
        open: price.open,
        high: price.high,
        low: price.low,
        close: price.close,
      })))
    } else {
      ;(series as ISeriesApi<'Area'>).setData(prices.map((price) => ({
        time: toTimestamp(price.time),
        value: price.close,
      })))
    }

    chartRef.current = chart
    seriesRef.current = series
    chart.timeScale().fitContent()

    const resizeObserver = new ResizeObserver(() => chart.applyOptions({ width: container.clientWidth }))
    resizeObserver.observe(container)

    return () => {
      resizeObserver.disconnect()
      drawingLinesRef.current = []
      riskLinesRef.current = []
      seriesRef.current = null
      chartRef.current = null
      chart.remove()
    }
  }, [prices, style])

  useEffect(() => {
    const series = seriesRef.current
    if (!series) return
    riskLinesRef.current.forEach((line) => series.removePriceLine(line))
    riskLinesRef.current = []

    if (takeProfit && takeProfit > 0) {
      riskLinesRef.current.push(series.createPriceLine({ price: takeProfit, color: '#34d399', lineWidth: 2, lineStyle: LineStyle.Dashed, axisLabelVisible: true, title: 'TP' }))
    }
    if (stopLoss && stopLoss > 0) {
      riskLinesRef.current.push(series.createPriceLine({ price: stopLoss, color: '#fb7185', lineWidth: 2, lineStyle: LineStyle.Dashed, axisLabelVisible: true, title: 'SL' }))
    }
  }, [prices, style, takeProfit, stopLoss])

  useEffect(() => {
    const chart = chartRef.current
    const series = seriesRef.current
    if (!chart || !series || tool === 'cursor') return

    const placeLine = (parameter: { point?: { x: number; y: number } }) => {
      if (!parameter.point) return
      const price = series.coordinateToPrice(parameter.point.y)
      if (price === null) return
      const appearance = tool === 'take-profit'
        ? { color: '#34d399', title: 'TP' }
        : tool === 'stop-loss'
          ? { color: '#fb7185', title: 'SL' }
          : { color: '#8ba5ff', title: 'Line' }
      drawingLinesRef.current.push(series.createPriceLine({ price, color: appearance.color, lineWidth: 2, lineStyle: LineStyle.Dashed, axisLabelVisible: true, title: appearance.title }))
      setDrawingCount(drawingLinesRef.current.length)
      setTool('cursor')
    }

    chart.subscribeClick(placeLine)
    return () => chart.unsubscribeClick(placeLine)
  }, [tool, prices, style])

  function clearDrawings() {
    const series = seriesRef.current
    if (series) drawingLinesRef.current.forEach((line) => series.removePriceLine(line))
    drawingLinesRef.current = []
    setDrawingCount(0)
    setTool('cursor')
  }

  return (
    <div className="trading-chart">
      <div className="trading-chart__toolbar" aria-label="Chart toolbar">
        <div className="trading-chart__group">
          <ToolbarButton active={style === 'candles'} label="Candles" onClick={() => setStyle('candles')} icon="candles" />
          <ToolbarButton active={style === 'area'} label="Area chart" onClick={() => setStyle('area')} icon="area" />
        </div>
        <span className="trading-chart__divider" />
        <div className="trading-chart__group">
          {(Object.keys(toolLabels) as DrawingTool[]).map((value) => <ToolbarButton key={value} active={tool === value} label={toolLabels[value]} onClick={() => setTool(value)} icon={value} />)}
        </div>
        <span className="trading-chart__divider" />
        <div className="trading-chart__group">
          <ToolbarButton label="Reset view" onClick={() => chartRef.current?.timeScale().fitContent()} icon="reset" />
          <ToolbarButton label={`Clear drawings${drawingCount ? ` (${drawingCount})` : ''}`} onClick={clearDrawings} icon="trash" disabled={!drawingCount} />
        </div>
      </div>
      {tool !== 'cursor' && <p className="trading-chart__hint">Click the chart to place a {toolLabels[tool].toLowerCase()}.</p>}
      <div ref={containerRef} aria-label="Interactive historical price chart" className={tool !== 'cursor' ? 'cursor-crosshair' : ''} />
    </div>
  )
}

function ToolbarButton({ active = false, disabled = false, label, icon, onClick }: { active?: boolean; disabled?: boolean; label: string; icon: string; onClick: () => void }) {
  return <button type="button" disabled={disabled} aria-label={label} aria-pressed={active} title={label} onClick={onClick} className={`chart-tool${active ? ' chart-tool--active' : ''}`}><ChartIcon name={icon} /><span>{label}</span></button>
}

function ChartIcon({ name }: { name: string }) {
  if (name === 'candles') return <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M7 3v18M4.5 7h5v8h-5zM17 2v20M14.5 10h5v7h-5z" /></svg>
  if (name === 'area') return <svg viewBox="0 0 24 24" aria-hidden="true"><path d="m3 18 5-6 4 3 7-9 2 2v11H3z" /></svg>
  if (name === 'cursor') return <svg viewBox="0 0 24 24" aria-hidden="true"><path d="m5 3 13 8-6 2-3 6z" /></svg>
  if (name === 'line') return <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M3 12h18M6 9v6M18 9v6" /></svg>
  if (name === 'take-profit') return <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M12 19V5m-5 5 5-5 5 5" /></svg>
  if (name === 'stop-loss') return <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M12 5v14m-5-5 5 5 5-5" /></svg>
  if (name === 'reset') return <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M4 11a8 8 0 1 1 2 6M4 4v7h7" /></svg>
  return <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M4 7h16M9 7V4h6v3m-9 0 1 13h10l1-13M10 11v5m4-5v5" /></svg>
}

function toTimestamp(value: string) {
  return Math.floor(new Date(value).getTime() / 1000) as UTCTimestamp
}
