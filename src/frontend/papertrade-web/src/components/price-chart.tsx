import {
  AreaSeries,
  CandlestickSeries,
  ColorType,
  CrosshairMode,
  HistogramSeries,
  LineSeries,
  LineStyle,
  createChart,
  type IChartApi,
  type IPriceLine,
  type ISeriesApi,
  type UTCTimestamp,
} from 'lightweight-charts'
import { useEffect, useRef, useState, type PointerEvent as ReactPointerEvent } from 'react'
import {
  AreaChart,
  ArrowUpRight,
  BarChart3,
  CandlestickChart,
  ChartSpline,
  Circle,
  Columns3,
  Crosshair,
  Eye,
  EyeOff,
  GripVertical,
  Lock,
  Maximize,
  Minus,
  MousePointer2,
  MoveDiagonal2,
  RectangleHorizontal,
  Rows3,
  Ruler,
  Star,
  Target,
  Trash2,
  TrendingDown,
  TrendingUp,
  Type,
  Unlock,
  type LucideIcon,
} from 'lucide-react'
import type { HistoricalPrice } from '../features/markets/market-types'

type ChartStyle = 'candles' | 'area'
type DrawingTool =
  | 'cursor'
  | 'crosshair'
  | 'line'
  | 'vertical-line'
  | 'trend'
  | 'ray'
  | 'arrow'
  | 'rectangle'
  | 'ellipse'
  | 'parallel-channel'
  | 'fib'
  | 'price-range'
  | 'long-position'
  | 'short-position'
  | 'text'
  | 'take-profit'
  | 'stop-loss'
type Indicator = 'sma' | 'ema' | 'bollinger' | 'volume'
type OverlayTool = Exclude<DrawingTool, 'cursor' | 'crosshair' | 'line' | 'take-profit' | 'stop-loss'>
type OverlayDrawing = { type: OverlayTool; start: Point; end: Point; label?: string }
type Point = { x: number; y: number }
type FloatingPosition = { x: number; y: number }
type DrawingToolDefinition = { id: DrawingTool; label: string; icon: LucideIcon; favorite?: boolean; dividerBefore?: boolean }

type PriceChartProps = {
  prices: HistoricalPrice[]
  takeProfit?: number | null
  stopLoss?: number | null
}

const toolLabels: Record<DrawingTool, string> = {
  cursor: 'Pointer',
  crosshair: 'Precision crosshair',
  line: 'Horizontal line',
  'vertical-line': 'Vertical line',
  trend: 'Trend line',
  ray: 'Trend ray',
  arrow: 'Arrow line',
  rectangle: 'Rectangle',
  ellipse: 'Ellipse',
  'parallel-channel': 'Parallel channel',
  fib: 'Fibonacci retracement',
  'price-range': 'Price range',
  'long-position': 'Long position',
  'short-position': 'Short position',
  text: 'Text note',
  'take-profit': 'Take profit',
  'stop-loss': 'Stop loss',
}

const drawingTools: DrawingToolDefinition[] = [
  { id: 'cursor', label: toolLabels.cursor, icon: MousePointer2 },
  { id: 'crosshair', label: toolLabels.crosshair, icon: Crosshair, favorite: true },
  { id: 'trend', label: toolLabels.trend, icon: MoveDiagonal2, favorite: true, dividerBefore: true },
  { id: 'ray', label: toolLabels.ray, icon: TrendingUp, favorite: true },
  { id: 'arrow', label: toolLabels.arrow, icon: ArrowUpRight, favorite: true },
  { id: 'line', label: toolLabels.line, icon: Minus, favorite: true },
  { id: 'vertical-line', label: toolLabels['vertical-line'], icon: Columns3, favorite: true },
  { id: 'rectangle', label: toolLabels.rectangle, icon: RectangleHorizontal, favorite: true, dividerBefore: true },
  { id: 'ellipse', label: toolLabels.ellipse, icon: Circle, favorite: true },
  { id: 'parallel-channel', label: toolLabels['parallel-channel'], icon: Rows3, favorite: true },
  { id: 'fib', label: toolLabels.fib, icon: Ruler, favorite: true },
  { id: 'price-range', label: toolLabels['price-range'], icon: Maximize, favorite: true, dividerBefore: true },
  { id: 'long-position', label: toolLabels['long-position'], icon: TrendingUp, favorite: true },
  { id: 'short-position', label: toolLabels['short-position'], icon: TrendingDown, favorite: true },
  { id: 'text', label: toolLabels.text, icon: Type, favorite: true },
  { id: 'take-profit', label: toolLabels['take-profit'], icon: Target, favorite: true, dividerBefore: true },
  { id: 'stop-loss', label: toolLabels['stop-loss'], icon: TrendingDown, favorite: true },
]

const overlayTools = new Set<DrawingTool>([
  'vertical-line', 'trend', 'ray', 'arrow', 'rectangle', 'ellipse',
  'parallel-channel', 'fib', 'price-range', 'long-position',
  'short-position', 'text',
])

const favoriteStorageKey = 'papertrade.chart.favorite-tools'
const toolbarPositionStorageKey = 'papertrade.chart.favorite-toolbar-position'

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
  const [indicators, setIndicators] = useState<Set<Indicator>>(new Set(['volume']))
  const [indicatorMenuOpen, setIndicatorMenuOpen] = useState(false)
  const [favoriteTools, setFavoriteTools] = useState<DrawingTool[]>(readFavoriteTools)
  const [toolbarPosition, setToolbarPosition] = useState<FloatingPosition>(readToolbarPosition)
  const toolbarDragRef = useRef<{ offsetX: number; offsetY: number } | null>(null)

  useEffect(() => {
    window.localStorage.setItem(favoriteStorageKey, JSON.stringify(favoriteTools))
  }, [favoriteTools])

  useEffect(() => {
    window.localStorage.setItem(toolbarPositionStorageKey, JSON.stringify(toolbarPosition))
  }, [toolbarPosition])

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

    if (indicators.has('sma')) {
      const sma = chart.addSeries(LineSeries, { color: '#f7e347', lineWidth: 2, priceLineVisible: false, lastValueVisible: false })
      sma.setData(calculateSma(prices, 20))
    }
    if (indicators.has('ema')) {
      const ema = chart.addSeries(LineSeries, { color: '#79a7ff', lineWidth: 2, priceLineVisible: false, lastValueVisible: false })
      ema.setData(calculateEma(prices, 20))
    }
    if (indicators.has('bollinger')) {
      const bands = calculateBollinger(prices, 20, 2)
      const upper = chart.addSeries(LineSeries, { color: '#c084fc', lineWidth: 1, lineStyle: LineStyle.Dashed, priceLineVisible: false, lastValueVisible: false })
      const lower = chart.addSeries(LineSeries, { color: '#c084fc', lineWidth: 1, lineStyle: LineStyle.Dashed, priceLineVisible: false, lastValueVisible: false })
      upper.setData(bands.upper)
      lower.setData(bands.lower)
    }
    if (indicators.has('volume')) {
      const volume = chart.addSeries(HistogramSeries, { priceScaleId: 'volume', priceFormat: { type: 'volume' }, priceLineVisible: false, lastValueVisible: false })
      volume.setData(prices.map((price) => ({ time: toTimestamp(price.time), value: price.volume, color: price.close >= price.open ? 'rgba(74, 222, 128, .36)' : 'rgba(255, 102, 143, .36)' })))
      chart.priceScale('volume').applyOptions({ scaleMargins: { top: 0.82, bottom: 0 } })
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
  }, [indicators, prices, style])

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
    if (drawingsLocked || !overlayTools.has(tool)) return
    const bounds = event.currentTarget.getBoundingClientRect()
    const point = { x: event.clientX - bounds.left, y: event.clientY - bounds.top }

    if (tool === 'vertical-line') {
      setOverlayDrawings((drawings) => [...drawings, { type: tool, start: point, end: point }])
      setTool('cursor')
      return
    }

    if (tool === 'text') {
      const label = window.prompt('Enter chart note')?.trim()
      if (label) {
        setOverlayDrawings((drawings) => [...drawings, { type: tool, start: point, end: point, label }])
      }
      setTool('cursor')
      return
    }

    if (!drawingStart) {
      setDrawingStart(point)
      return
    }
    setOverlayDrawings((drawings) => [...drawings, { type: tool as OverlayTool, start: drawingStart, end: point }])
    setDrawingStart(null)
    setTool('cursor')
  }

  const drawingCount = priceDrawingCount + overlayDrawings.length

  function toggleIndicator(indicator: Indicator) {
    setIndicators((current) => {
      const next = new Set(current)
      if (next.has(indicator)) next.delete(indicator)
      else next.add(indicator)
      return next
    })
  }

  function selectTool(selectedTool: DrawingTool) {
    setDrawingStart(null)
    setTool(selectedTool)
  }

  function toggleFavorite(selectedTool: DrawingTool) {
    setFavoriteTools((current) => current.includes(selectedTool)
      ? current.filter((item) => item !== selectedTool)
      : [...current, selectedTool])
  }

  function startToolbarDrag(event: ReactPointerEvent<HTMLButtonElement>) {
    const toolbar = event.currentTarget.parentElement
    if (!toolbar) return
    const bounds = toolbar.getBoundingClientRect()
    toolbarDragRef.current = {
      offsetX: event.clientX - bounds.left,
      offsetY: event.clientY - bounds.top,
    }
    event.currentTarget.setPointerCapture(event.pointerId)
  }

  function dragToolbar(event: ReactPointerEvent<HTMLButtonElement>) {
    const drag = toolbarDragRef.current
    const container = containerRef.current
    if (!drag || !container) return
    const bounds = container.getBoundingClientRect()
    const toolbar = event.currentTarget.parentElement
    const width = toolbar?.clientWidth ?? 0
    const height = toolbar?.clientHeight ?? 0
    setToolbarPosition({
      x: clamp(event.clientX - bounds.left - drag.offsetX, 58, Math.max(58, bounds.width - width - 8)),
      y: clamp(event.clientY - bounds.top - drag.offsetY, 8, Math.max(8, bounds.height - height - 8)),
    })
  }

  function stopToolbarDrag(event: ReactPointerEvent<HTMLButtonElement>) {
    toolbarDragRef.current = null
    if (event.currentTarget.hasPointerCapture(event.pointerId)) {
      event.currentTarget.releasePointerCapture(event.pointerId)
    }
  }

  return (
    <div className="trading-chart">
      <aside className="drawing-rail" aria-label="Drawing tools">
        {drawingTools.map((definition) => {
          const Icon = definition.icon
          const isFavorite = favoriteTools.includes(definition.id)
          return <div key={definition.id} className={`drawing-rail__item${definition.dividerBefore ? ' drawing-rail__item--divided' : ''}`}>
            <ToolbarButton active={tool === definition.id} label={definition.label} onClick={() => selectTool(definition.id)} icon={<Icon />} />
            {definition.favorite && <button type="button" className={`drawing-favorite${isFavorite ? ' is-favorite' : ''}`} aria-label={`${isFavorite ? 'Remove' : 'Add'} ${definition.label} ${isFavorite ? 'from' : 'to'} favorites`} aria-pressed={isFavorite} title={`${isFavorite ? 'Remove from' : 'Add to'} favorites`} onClick={() => toggleFavorite(definition.id)}><Star /></button>}
          </div>
        })}
        <span className="drawing-rail__separator" />
        <ToolbarButton active={style === 'candles'} label="Candles" onClick={() => setStyle('candles')} icon={<CandlestickChart />} />
        <ToolbarButton active={style === 'area'} label="Area chart" onClick={() => setStyle('area')} icon={<AreaChart />} />
        <ToolbarButton active={indicatorMenuOpen} label="Indicators" onClick={() => setIndicatorMenuOpen((value) => !value)} icon={<ChartSpline />} />
        <ToolbarButton label="Fit chart" onClick={() => chartRef.current?.timeScale().fitContent()} icon={<Maximize />} />
        <ToolbarButton active={drawingsLocked} label={drawingsLocked ? 'Unlock drawings' : 'Lock drawings'} onClick={() => setDrawingsLocked((value) => !value)} icon={drawingsLocked ? <Lock /> : <Unlock />} />
        <ToolbarButton active={!drawingsVisible} label={drawingsVisible ? 'Hide drawings' : 'Show drawings'} onClick={() => setDrawingsVisible((value) => !value)} icon={drawingsVisible ? <Eye /> : <EyeOff />} />
        <ToolbarButton label={`Clear drawings${drawingCount ? ` (${drawingCount})` : ''}`} onClick={clearDrawings} icon={<Trash2 />} disabled={!drawingCount} />
      </aside>

      {favoriteTools.length > 0 && <div className="floating-chart-toolbar" aria-label="Favorite drawing tools" style={{ left: toolbarPosition.x, top: toolbarPosition.y }}>
        <button type="button" className="floating-chart-toolbar__drag" aria-label="Drag favorite tools" title="Drag toolbar" onPointerDown={startToolbarDrag} onPointerMove={dragToolbar} onPointerUp={stopToolbarDrag} onPointerCancel={stopToolbarDrag}><GripVertical /></button>
        {favoriteTools.map((favoriteTool) => {
          const definition = drawingTools.find((item) => item.id === favoriteTool)
          if (!definition) return null
          const Icon = definition.icon
          return <ToolbarButton key={favoriteTool} active={tool === favoriteTool} label={definition.label} onClick={() => selectTool(favoriteTool)} icon={<Icon />} />
        })}
      </div>}
      {indicatorMenuOpen && <div className="indicator-menu" role="group" aria-label="Technical indicators">
        <div><strong>Indicators</strong><span>Applied to this chart</span></div>
        <IndicatorToggle label="SMA 20" description="Simple moving average" active={indicators.has('sma')} onClick={() => toggleIndicator('sma')} icon={<ChartSpline />} />
        <IndicatorToggle label="EMA 20" description="Exponential moving average" active={indicators.has('ema')} onClick={() => toggleIndicator('ema')} icon={<ChartSpline />} />
        <IndicatorToggle label="Bollinger" description="20 period · 2σ" active={indicators.has('bollinger')} onClick={() => toggleIndicator('bollinger')} icon={<Rows3 />} />
        <IndicatorToggle label="Volume" description="Trade volume" active={indicators.has('volume')} onClick={() => toggleIndicator('volume')} icon={<BarChart3 />} />
      </div>}
      {tool !== 'cursor' && <p className="trading-chart__hint">Click the chart to place a {toolLabels[tool].toLowerCase()}.</p>}
      <div ref={containerRef} aria-label="Interactive historical price chart" className="chart-canvas" />
      <svg className={`chart-drawing-overlay${overlayTools.has(tool) ? ' is-drawing' : ''}`} onClick={drawOnOverlay} aria-hidden="true">
        <defs>
          <marker id="chart-arrow" viewBox="0 0 10 10" refX="9" refY="5" markerWidth="6" markerHeight="6" orient="auto-start-reverse">
            <path d="M 0 0 L 10 5 L 0 10 z" />
          </marker>
        </defs>
        {drawingsVisible && overlayDrawings.map((drawing, index) => <OverlayShape key={index} drawing={drawing} />)}
        {drawingStart && <circle cx={drawingStart.x} cy={drawingStart.y} r="5" />}
      </svg>
    </div>
  )
}

function ToolbarButton({ active = false, disabled = false, label, icon, onClick }: { active?: boolean; disabled?: boolean; label: string; icon: React.ReactNode; onClick: () => void }) {
  return <button type="button" disabled={disabled} aria-label={label} aria-pressed={active} title={label} onClick={onClick} className={`chart-tool${active ? ' chart-tool--active' : ''}`}>{icon}</button>
}

function IndicatorToggle({ label, description, active, onClick, icon }: { label: string; description: string; active: boolean; onClick: () => void; icon: React.ReactNode }) {
  return <button type="button" aria-pressed={active} onClick={onClick} className={active ? 'is-active' : ''}><span className="indicator-menu__icon">{icon}</span><span><strong>{label}</strong><small>{description}</small></span><i>{active ? 'ON' : 'OFF'}</i></button>
}

function OverlayShape({ drawing }: { drawing: OverlayDrawing }) {
  const left = Math.min(drawing.start.x, drawing.end.x)
  const top = Math.min(drawing.start.y, drawing.end.y)
  const width = Math.abs(drawing.end.x - drawing.start.x)
  const height = Math.abs(drawing.end.y - drawing.start.y)

  switch (drawing.type) {
    case 'trend':
      return <line x1={drawing.start.x} y1={drawing.start.y} x2={drawing.end.x} y2={drawing.end.y} />
    case 'ray':
      return <line className="drawing-ray" x1={drawing.start.x} y1={drawing.start.y} x2={drawing.start.x + (drawing.end.x - drawing.start.x) * 8} y2={drawing.start.y + (drawing.end.y - drawing.start.y) * 8} />
    case 'arrow':
      return <line className="drawing-arrow" x1={drawing.start.x} y1={drawing.start.y} x2={drawing.end.x} y2={drawing.end.y} markerEnd="url(#chart-arrow)" />
    case 'vertical-line':
      return <line className="drawing-vertical" x1={drawing.start.x} y1="0" x2={drawing.start.x} y2="100%" />
    case 'rectangle':
      return <rect x={left} y={top} width={width} height={height} />
    case 'ellipse':
      return <ellipse cx={left + width / 2} cy={top + height / 2} rx={width / 2} ry={height / 2} />
    case 'parallel-channel':
      return <g className="parallel-channel"><line x1={drawing.start.x} y1={drawing.start.y} x2={drawing.end.x} y2={drawing.end.y} /><line x1={drawing.start.x} y1={drawing.start.y + 24} x2={drawing.end.x} y2={drawing.end.y + 24} /></g>
    case 'fib':
      return <FibonacciDrawing drawing={drawing} />
    case 'price-range':
      return <g className="price-range-drawing"><rect x={left} y={top} width={width} height={height} /><line x1={left} y1={top} x2={left + width} y2={top + height} /><text x={left + 6} y={top + 16}>PRICE RANGE</text></g>
    case 'long-position':
    case 'short-position':
      return <PositionDrawing drawing={drawing} />
    case 'text':
      return <g className="text-drawing"><rect x={drawing.start.x - 4} y={drawing.start.y - 18} width={Math.max(52, (drawing.label?.length ?? 0) * 7 + 12)} height="25" /><text x={drawing.start.x + 3} y={drawing.start.y}>{drawing.label}</text></g>
  }
}

function PositionDrawing({ drawing }: { drawing: OverlayDrawing }) {
  const left = Math.min(drawing.start.x, drawing.end.x)
  const top = Math.min(drawing.start.y, drawing.end.y)
  const width = Math.abs(drawing.end.x - drawing.start.x)
  const height = Math.abs(drawing.end.y - drawing.start.y)
  const middle = top + height / 2
  const isLong = drawing.type === 'long-position'
  return <g className={`position-drawing ${isLong ? 'is-long' : 'is-short'}`}>
    <rect className="position-drawing__reward" x={left} y={isLong ? top : middle} width={width} height={height / 2} />
    <rect className="position-drawing__risk" x={left} y={isLong ? middle : top} width={width} height={height / 2} />
    <line x1={left} y1={middle} x2={left + width} y2={middle} />
    <text x={left + 6} y={middle - 6}>{isLong ? 'LONG' : 'SHORT'} ENTRY</text>
  </g>
}

function FibonacciDrawing({ drawing }: { drawing: OverlayDrawing }) {
  const levels = [0, 0.236, 0.382, 0.5, 0.618, 0.786, 1]
  const left = Math.min(drawing.start.x, drawing.end.x)
  const right = Math.max(drawing.start.x, drawing.end.x)
  return <g className="fibonacci-drawing">{levels.map((level) => {
    const y = drawing.start.y + (drawing.end.y - drawing.start.y) * level
    return <g key={level}><line x1={left} y1={y} x2={right} y2={y} /><text x={right + 4} y={y - 3}>{level}</text></g>
  })}</g>
}

function toTimestamp(value: string) {
  return Math.floor(new Date(value).getTime() / 1000) as UTCTimestamp
}

function calculateSma(prices: HistoricalPrice[], period: number) {
  return prices.flatMap((price, index) => {
    if (index < period - 1) return []
    const values = prices.slice(index - period + 1, index + 1)
    return [{ time: toTimestamp(price.time), value: values.reduce((sum, item) => sum + item.close, 0) / period }]
  })
}

function calculateEma(prices: HistoricalPrice[], period: number) {
  if (!prices.length) return []
  const multiplier = 2 / (period + 1)
  let ema = prices[0].close
  return prices.map((price) => {
    ema = price.close * multiplier + ema * (1 - multiplier)
    return { time: toTimestamp(price.time), value: ema }
  })
}

function calculateBollinger(prices: HistoricalPrice[], period: number, deviations: number) {
  const upper: { time: UTCTimestamp; value: number }[] = []
  const lower: { time: UTCTimestamp; value: number }[] = []
  prices.forEach((price, index) => {
    if (index < period - 1) return
    const values = prices.slice(index - period + 1, index + 1).map((item) => item.close)
    const mean = values.reduce((sum, value) => sum + value, 0) / period
    const standardDeviation = Math.sqrt(values.reduce((sum, value) => sum + (value - mean) ** 2, 0) / period)
    const time = toTimestamp(price.time)
    upper.push({ time, value: mean + standardDeviation * deviations })
    lower.push({ time, value: mean - standardDeviation * deviations })
  })
  return { upper, lower }
}

function readFavoriteTools(): DrawingTool[] {
  try {
    const stored = JSON.parse(window.localStorage.getItem(favoriteStorageKey) ?? '[]')
    if (!Array.isArray(stored)) return []
    return stored.filter((value): value is DrawingTool =>
      typeof value === 'string' && drawingTools.some((tool) => tool.id === value && tool.favorite))
  } catch {
    return []
  }
}

function readToolbarPosition(): FloatingPosition {
  try {
    const stored = JSON.parse(window.localStorage.getItem(toolbarPositionStorageKey) ?? '{}')
    if (Number.isFinite(stored.x) && Number.isFinite(stored.y)) {
      return { x: stored.x, y: stored.y }
    }
  } catch {
    // Use the default position when saved preferences cannot be read.
  }
  return { x: 180, y: 12 }
}

function clamp(value: number, minimum: number, maximum: number) {
  return Math.min(Math.max(value, minimum), maximum)
}
