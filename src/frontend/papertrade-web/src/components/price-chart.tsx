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
import {
  AreaChart,
  CandlestickChart,
  Crosshair,
  Eye,
  EyeOff,
  Lock,
  Maximize,
  Minus,
  MousePointer2,
  MoveDiagonal2,
  RectangleHorizontal,
  RotateCcw,
  Target,
  Trash2,
  TrendingDown,
  TrendingUp,
  Unlock,
} from 'lucide-react'
import type { HistoricalPrice } from '../features/markets/market-types'

type ChartStyle = 'candles' | 'area'
type DrawingTool = 'cursor' | 'crosshair' | 'line' | 'trend' | 'rectangle' | 'take-profit' | 'stop-loss'
type OverlayDrawing = { type: 'trend' | 'rectangle'; start: Point; end: Point }
type Point = { x: number; y: number }

type PriceChartProps = {
  prices: HistoricalPrice[]
  takeProfit?: number | null
  stopLoss?: number | null
}

const toolLabels: Record<DrawingTool, string> = {
  cursor: 'Crosshair',
  crosshair: 'Precision crosshair',
  line: 'Horizontal line',
  trend: 'Trend line',
  rectangle: 'Rectangle',
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
  const [priceDrawingCount, setPriceDrawingCount] = useState(0)
  const [overlayDrawings, setOverlayDrawings] = useState<OverlayDrawing[]>([])
  const [drawingStart, setDrawingStart] = useState<Point | null>(null)
  const [drawingsVisible, setDrawingsVisible] = useState(true)
  const [drawingsLocked, setDrawingsLocked] = useState(false)

  useEffect(() => {
    const container = containerRef.current
    if (!container) return

    const chart: IChartApi = createChart(container, {
      height: container.clientHeight || 620,
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

    const resizeObserver = new ResizeObserver(() => chart.applyOptions({ width: container.clientWidth, height: container.clientHeight }))
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
    if (!chart || !series || !['line', 'take-profit', 'stop-loss'].includes(tool) || drawingsLocked) return

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
      setPriceDrawingCount(drawingLinesRef.current.length)
      setTool('cursor')
    }

    chart.subscribeClick(placeLine)
    return () => chart.unsubscribeClick(placeLine)
  }, [tool, prices, style, drawingsLocked])

  function clearDrawings() {
    const series = seriesRef.current
    if (series) drawingLinesRef.current.forEach((line) => series.removePriceLine(line))
    drawingLinesRef.current = []
    setPriceDrawingCount(0)
    setOverlayDrawings([])
    setDrawingStart(null)
    setTool('cursor')
  }

  function drawOnOverlay(event: React.MouseEvent<SVGSVGElement>) {
    if (drawingsLocked || (tool !== 'trend' && tool !== 'rectangle')) return
    const bounds = event.currentTarget.getBoundingClientRect()
    const point = { x: event.clientX - bounds.left, y: event.clientY - bounds.top }
    if (!drawingStart) {
      setDrawingStart(point)
      return
    }
    setOverlayDrawings((drawings) => [...drawings, { type: tool, start: drawingStart, end: point }])
    setDrawingStart(null)
    setTool('cursor')
  }

  const drawingCount = priceDrawingCount + overlayDrawings.length

  return (
    <div className="trading-chart">
      <aside className="drawing-rail" aria-label="Drawing tools">
        <ToolbarButton active={tool === 'cursor'} label="Pointer" onClick={() => setTool('cursor')} icon={<MousePointer2 />} />
        <ToolbarButton active={tool === 'crosshair'} label="Crosshair" onClick={() => setTool('crosshair')} icon={<Crosshair />} />
        <span className="drawing-rail__separator" />
        <ToolbarButton active={tool === 'trend'} label="Trend line" onClick={() => setTool('trend')} icon={<MoveDiagonal2 />} />
        <ToolbarButton active={tool === 'line'} label="Horizontal line" onClick={() => setTool('line')} icon={<Minus />} />
        <ToolbarButton active={tool === 'rectangle'} label="Rectangle" onClick={() => setTool('rectangle')} icon={<RectangleHorizontal />} />
        <span className="drawing-rail__separator" />
        <ToolbarButton active={tool === 'take-profit'} label="Take profit line" onClick={() => setTool('take-profit')} icon={<TrendingUp />} />
        <ToolbarButton active={tool === 'stop-loss'} label="Stop loss line" onClick={() => setTool('stop-loss')} icon={<TrendingDown />} />
        <ToolbarButton label="Fit chart" onClick={() => chartRef.current?.timeScale().fitContent()} icon={<Maximize />} />
        <span className="drawing-rail__separator" />
        <ToolbarButton active={drawingsLocked} label={drawingsLocked ? 'Unlock drawings' : 'Lock drawings'} onClick={() => setDrawingsLocked((value) => !value)} icon={drawingsLocked ? <Lock /> : <Unlock />} />
        <ToolbarButton active={!drawingsVisible} label={drawingsVisible ? 'Hide drawings' : 'Show drawings'} onClick={() => setDrawingsVisible((value) => !value)} icon={drawingsVisible ? <Eye /> : <EyeOff />} />
        <ToolbarButton label={`Clear drawings${drawingCount ? ` (${drawingCount})` : ''}`} onClick={clearDrawings} icon={<Trash2 />} disabled={!drawingCount} />
      </aside>

      <div className="floating-chart-toolbar" aria-label="Chart controls">
        <ToolbarButton active={style === 'candles'} label="Candles" onClick={() => setStyle('candles')} icon={<CandlestickChart />} />
        <ToolbarButton active={style === 'area'} label="Area chart" onClick={() => setStyle('area')} icon={<AreaChart />} />
        <span />
        <ToolbarButton active={tool === 'crosshair'} label="Crosshair" onClick={() => setTool('crosshair')} icon={<Crosshair />} />
        <ToolbarButton active={tool === 'trend'} label="Trend line" onClick={() => setTool('trend')} icon={<MoveDiagonal2 />} />
        <ToolbarButton active={tool === 'line'} label="Horizontal line" onClick={() => setTool('line')} icon={<Minus />} />
        <ToolbarButton active={tool === 'rectangle'} label="Rectangle" onClick={() => setTool('rectangle')} icon={<RectangleHorizontal />} />
        <ToolbarButton active={tool === 'take-profit'} label="Take profit" onClick={() => setTool('take-profit')} icon={<Target />} />
        <ToolbarButton label="Reset view" onClick={() => chartRef.current?.timeScale().fitContent()} icon={<RotateCcw />} />
      </div>
      {tool !== 'cursor' && <p className="trading-chart__hint">Click the chart to place a {toolLabels[tool].toLowerCase()}.</p>}
      <div ref={containerRef} aria-label="Interactive historical price chart" className="chart-canvas" />
      <svg className={`chart-drawing-overlay${tool === 'trend' || tool === 'rectangle' ? ' is-drawing' : ''}`} onClick={drawOnOverlay} aria-hidden="true">
        {drawingsVisible && overlayDrawings.map((drawing, index) => drawing.type === 'trend'
          ? <line key={index} x1={drawing.start.x} y1={drawing.start.y} x2={drawing.end.x} y2={drawing.end.y} />
          : <rect key={index} x={Math.min(drawing.start.x, drawing.end.x)} y={Math.min(drawing.start.y, drawing.end.y)} width={Math.abs(drawing.end.x - drawing.start.x)} height={Math.abs(drawing.end.y - drawing.start.y)} />)}
        {drawingStart && <circle cx={drawingStart.x} cy={drawingStart.y} r="5" />}
      </svg>
    </div>
  )
}

function ToolbarButton({ active = false, disabled = false, label, icon, onClick }: { active?: boolean; disabled?: boolean; label: string; icon: React.ReactNode; onClick: () => void }) {
  return <button type="button" disabled={disabled} aria-label={label} aria-pressed={active} title={label} onClick={onClick} className={`chart-tool${active ? ' chart-tool--active' : ''}`}>{icon}</button>
}

function toTimestamp(value: string) {
  return Math.floor(new Date(value).getTime() / 1000) as UTCTimestamp
}
