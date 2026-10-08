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
  type IRange,
  type Time,
  type Logical,
  type MouseEventParams,
} from 'lightweight-charts'
import { useEffect, useId, useRef, useState, type PointerEvent as ReactPointerEvent } from 'react'
import {
  AreaChart,
  ArrowLeftRight,
  ArrowUpRight,
  BarChart3,
  CandlestickChart,
  ChartSpline,
  ChevronRight,
  Circle,
  Columns3,
  Crosshair,
  Eye,
  EyeOff,
  GripVertical,
  GitFork,
  Lock,
  Magnet,
  Maximize,
  MessageSquare,
  Minus,
  MousePointer2,
  MoveDiagonal2,
  RectangleHorizontal,
  Rows3,
  Ruler,
  Star,
  Target,
  Triangle,
  Trash2,
  TrendingDown,
  TrendingUp,
  Type,
  Unlock,
  type LucideIcon,
} from 'lucide-react'
import type { HistoricalPrice } from '../features/markets/market-types'
import type { Instrument } from '../features/markets/market-types'

type ChartStyle = 'candles' | 'area'
type DrawingTool =
  | 'cursor'
  | 'crosshair'
  | 'line'
  | 'vertical-line'
  | 'trend'
  | 'ray'
  | 'extended-line'
  | 'arrow'
  | 'rectangle'
  | 'ellipse'
  | 'triangle'
  | 'parallel-channel'
  | 'pitchfork'
  | 'fib'
  | 'price-range'
  | 'date-range'
  | 'long-position'
  | 'short-position'
  | 'text'
  | 'callout'
  | 'take-profit'
  | 'stop-loss'
type Indicator = 'sma' | 'ema' | 'bollinger' | 'volume'
type OverlayTool = Exclude<DrawingTool, 'cursor' | 'crosshair'>
export type OverlayDrawing = { id: string; type: OverlayTool; start: Point; end: Point; third?: Point; label?: string }
export type ChartLayoutState = { style: ChartStyle; indicators: Indicator[]; drawings: OverlayDrawing[]; drawingsVisible: boolean; secondarySymbol?: string; candleInterval?: import('../features/markets/market-types').CandleInterval }
// Keep legacy x/y for saved layouts, but new drawings are anchored to market data.
type Point = { x: number; y: number; time?: number; price?: number }
type FloatingPosition = { x: number; y: number }
type DrawingToolDefinition = { id: DrawingTool; label: string; icon: LucideIcon; favorite?: boolean }
type DrawingToolGroup = { id: string; label: string; tools: DrawingTool[] }
type DrawingDrag = { id: string; mode: 'move' | 'start' | 'end' | 'third'; pointer: Point; original: OverlayDrawing }

type PriceChartProps = {
  prices: HistoricalPrice[]
  instrument?: Instrument
  takeProfit?: number | null
  stopLoss?: number | null
  initialLayout?: ChartLayoutState
  onLayoutChange?: (state: ChartLayoutState) => void
  visibleRange?: IRange<Time> | null
  onVisibleRangeChange?: (range: IRange<Time> | null) => void
}

const toolLabels: Record<DrawingTool, string> = {
  cursor: 'Pointer',
  crosshair: 'Precision crosshair',
  line: 'Horizontal line',
  'vertical-line': 'Vertical line',
  trend: 'Trend line',
  ray: 'Trend ray',
  'extended-line': 'Extended line',
  arrow: 'Arrow line',
  rectangle: 'Rectangle',
  ellipse: 'Ellipse',
  triangle: 'Triangle',
  'parallel-channel': 'Parallel channel',
  pitchfork: 'Pitchfork',
  fib: 'Fibonacci retracement',
  'price-range': 'Price range',
  'date-range': 'Date range',
  'long-position': 'Long position',
  'short-position': 'Short position',
  text: 'Text note',
  callout: 'Callout',
  'take-profit': 'Take profit',
  'stop-loss': 'Stop loss',
}

const drawingTools: DrawingToolDefinition[] = [
  { id: 'cursor', label: toolLabels.cursor, icon: MousePointer2 },
  { id: 'crosshair', label: toolLabels.crosshair, icon: Crosshair, favorite: true },
  { id: 'trend', label: toolLabels.trend, icon: MoveDiagonal2, favorite: true },
  { id: 'ray', label: toolLabels.ray, icon: TrendingUp, favorite: true },
  { id: 'extended-line', label: toolLabels['extended-line'], icon: ArrowLeftRight, favorite: true },
  { id: 'arrow', label: toolLabels.arrow, icon: ArrowUpRight, favorite: true },
  { id: 'line', label: toolLabels.line, icon: Minus, favorite: true },
  { id: 'vertical-line', label: toolLabels['vertical-line'], icon: Columns3, favorite: true },
  { id: 'rectangle', label: toolLabels.rectangle, icon: RectangleHorizontal, favorite: true },
  { id: 'ellipse', label: toolLabels.ellipse, icon: Circle, favorite: true },
  { id: 'triangle', label: toolLabels.triangle, icon: Triangle, favorite: true },
  { id: 'parallel-channel', label: toolLabels['parallel-channel'], icon: Rows3, favorite: true },
  { id: 'pitchfork', label: toolLabels.pitchfork, icon: GitFork, favorite: true },
  { id: 'fib', label: toolLabels.fib, icon: Ruler, favorite: true },
  { id: 'price-range', label: toolLabels['price-range'], icon: Maximize, favorite: true },
  { id: 'date-range', label: toolLabels['date-range'], icon: Columns3, favorite: true },
  { id: 'long-position', label: toolLabels['long-position'], icon: TrendingUp, favorite: true },
  { id: 'short-position', label: toolLabels['short-position'], icon: TrendingDown, favorite: true },
  { id: 'text', label: toolLabels.text, icon: Type, favorite: true },
  { id: 'callout', label: toolLabels.callout, icon: MessageSquare, favorite: true },
  { id: 'take-profit', label: toolLabels['take-profit'], icon: Target, favorite: true },
  { id: 'stop-loss', label: toolLabels['stop-loss'], icon: TrendingDown, favorite: true },
]

const drawingToolGroups: DrawingToolGroup[] = [
  { id: 'cursor', label: 'Cursor tools', tools: ['cursor', 'crosshair'] },
  { id: 'lines', label: 'Line tools', tools: ['trend', 'ray', 'extended-line', 'arrow', 'line', 'vertical-line'] },
  { id: 'shapes', label: 'Shape tools', tools: ['rectangle', 'ellipse', 'triangle'] },
  { id: 'channels', label: 'Channel and Fibonacci tools', tools: ['parallel-channel', 'pitchfork', 'fib'] },
  { id: 'measure', label: 'Measurement and position tools', tools: ['price-range', 'date-range', 'long-position', 'short-position'] },
  { id: 'notes', label: 'Annotation tools', tools: ['text', 'callout'] },
  { id: 'risk', label: 'Risk level tools', tools: ['take-profit', 'stop-loss'] },
]

const overlayTools = new Set<DrawingTool>(drawingTools.map((tool) => tool.id).filter((tool) => tool !== 'cursor' && tool !== 'crosshair'))

const favoriteStorageKey = 'papertrade.chart.favorite-tools'
const toolbarPositionStorageKey = 'papertrade.chart.favorite-toolbar-position'

export function PriceChart({ prices, instrument, takeProfit, stopLoss, initialLayout, onLayoutChange, visibleRange, onVisibleRangeChange }: PriceChartProps) {
  const containerRef = useRef<HTMLDivElement>(null)
  const workspaceRef = useRef<HTMLDivElement>(null)
  const chartRef = useRef<IChartApi | null>(null)
  const seriesRef = useRef<ISeriesApi<'Candlestick'> | ISeriesApi<'Area'> | null>(null)
  const riskLinesRef = useRef<IPriceLine[]>([])
  const [style, setStyle] = useState<ChartStyle>(initialLayout?.style ?? 'candles')
  const [tool, setTool] = useState<DrawingTool>('cursor')
  const [overlayDrawings, setOverlayDrawings] = useState<OverlayDrawing[]>(initialLayout?.drawings ?? [])
  const [drawingStart, setDrawingStart] = useState<Point | null>(null)
  const [drawingPreview, setDrawingPreview] = useState<Point | null>(null)
  const [drawingSecond, setDrawingSecond] = useState<Point | null>(null)
  const [magnet, setMagnet] = useState(false)
  const [selectedDrawingId, setSelectedDrawingId] = useState<string | null>(null)
  const [drawingsVisible, setDrawingsVisible] = useState(initialLayout?.drawingsVisible ?? true)
  const [drawingsLocked, setDrawingsLocked] = useState(false)
  const [indicators, setIndicators] = useState<Set<Indicator>>(new Set(initialLayout?.indicators ?? ['volume']))
  const [indicatorMenuOpen, setIndicatorMenuOpen] = useState(false)
  const [favoriteTools, setFavoriteTools] = useState<DrawingTool[]>(readFavoriteTools)
  const [toolbarPosition, setToolbarPosition] = useState<FloatingPosition>(readToolbarPosition)
  const [openToolGroup, setOpenToolGroup] = useState<string | null>(null)
  const [toolMenuTop, setToolMenuTop] = useState(0)
  const [groupSelections, setGroupSelections] = useState<Record<string, DrawingTool>>(createInitialGroupSelections)
  const toolbarDragRef = useRef<{ offsetX: number; offsetY: number } | null>(null)
  const drawingDragRef = useRef<DrawingDrag | null>(null)
  const rangeCallbackRef = useRef(onVisibleRangeChange)
  const visibleRangeRef = useRef(visibleRange)
  const pricesRef = useRef(prices)
  useEffect(() => { rangeCallbackRef.current = onVisibleRangeChange; visibleRangeRef.current = visibleRange; pricesRef.current = prices }, [onVisibleRangeChange, visibleRange, prices])
  const updateDataRef = useRef<((data: HistoricalPrice[]) => void) | null>(null)
  const savedRangeRef = useRef<IRange<number> | null>(null)
  const [projection, setProjection] = useState({ x0: 0, barWidth: 0, y0: 0, priceScale: 0 })
  const [plotSize, setPlotSize] = useState({ width: 0, height: 0 })
  const [hoveredBar, setHoveredBar] = useState<HistoricalPrice | null>(null)
  const legacyDrawingsRef = useRef(Boolean(initialLayout?.drawings.some(drawing => drawing.start.time == null)))
  const clipId = useId()

  // Time-scale events don't cover price-axis drags. Check the transform once per
  // frame, rendering React only when the chart's coordinate system changes.
  useEffect(() => {
    let frame = 0
    let previous = ''
    function refresh() {
      const chart = chartRef.current
      const series = seriesRef.current
      if (chart && series) {
        const sample = pricesRef.current.at(-1)?.close ?? 1
        const signature = JSON.stringify([chart.timeScale().getVisibleLogicalRange(), chart.timeScale().width(), containerRef.current?.clientHeight, series.priceToCoordinate(sample), series.priceToCoordinate(sample * 1.01)])
        if (signature !== previous) {
          previous = signature
          const x0 = chart.timeScale().logicalToCoordinate(0 as Logical) ?? 0
          const x1 = chart.timeScale().logicalToCoordinate(1 as Logical) ?? 0
          const y0 = series.priceToCoordinate(0) ?? 0
          const y1 = series.priceToCoordinate(1) ?? 0
          setProjection({ x0, barWidth: x1 - x0, y0, priceScale: y1 - y0 })
          setPlotSize({ width: chart.timeScale().width(), height: (containerRef.current?.clientHeight ?? 0) - chart.timeScale().height() })
          if (legacyDrawingsRef.current && pricesRef.current.length && x1 !== x0) {
            legacyDrawingsRef.current = false
            const anchor = (point: Point) => ({ ...point, time: timeAtLogical(pricesRef.current, chart.timeScale().coordinateToLogical(point.x) ?? 0), price: series.coordinateToPrice(point.y) ?? 0 })
            setOverlayDrawings(drawings => drawings.map(drawing => drawing.start.time != null ? drawing : { ...drawing, start: anchor(drawing.start), end: anchor(drawing.end), third: drawing.third ? anchor(drawing.third) : undefined }))
          }
        }
      }
      frame = requestAnimationFrame(refresh)
    }
    frame = requestAnimationFrame(refresh)
    return () => cancelAnimationFrame(frame)
  }, [])

  useEffect(() => { updateDataRef.current?.(prices) }, [prices])

  useEffect(() => {
    chartRef.current?.applyOptions({ crosshair: { mode: tool === 'crosshair' || magnet ? CrosshairMode.Magnet : CrosshairMode.Normal } })
  }, [tool, magnet, style, indicators])

  useEffect(() => {
    onLayoutChange?.({ style, indicators: [...indicators], drawings: overlayDrawings, drawingsVisible })
  }, [style, indicators, overlayDrawings, drawingsVisible, onLayoutChange])

  useEffect(() => {
    if (visibleRange && chartRef.current && JSON.stringify(chartRef.current.timeScale().getVisibleRange()) !== JSON.stringify(visibleRange)) chartRef.current.timeScale().setVisibleRange(visibleRange)
  }, [visibleRange])

  useEffect(() => {
    window.localStorage.setItem(favoriteStorageKey, JSON.stringify(favoriteTools))
  }, [favoriteTools])

  useEffect(() => {
    window.localStorage.setItem(toolbarPositionStorageKey, JSON.stringify(toolbarPosition))
  }, [toolbarPosition])

  useEffect(() => {
    const container = containerRef.current
    if (!container) return
    const prices = pricesRef.current
    const updates: ((data: HistoricalPrice[]) => void)[] = []

    const chart: IChartApi = createChart(container, {
      height: container.clientHeight || 620,
      width: container.clientWidth,
      localization: { timeFormatter: (time: Time) => new Date(Number(time) * 1000).toISOString().replace('T', ' ').slice(0, 16) + ' UTC' },
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
          priceFormat: { type: 'price', precision: instrument?.pricePrecision ?? 2, minMove: instrument?.tickSize ?? 0.01 },
        })
      : chart.addSeries(AreaSeries, {
          lineColor: '#7ddcff',
          lineWidth: 2,
          topColor: 'rgba(125, 220, 255, 0.32)',
          bottomColor: 'rgba(128, 143, 255, 0.015)',
          priceLineColor: '#7ddcff',
          priceLineStyle: LineStyle.Dotted,
          priceFormat: { type: 'price', precision: instrument?.pricePrecision ?? 2, minMove: instrument?.tickSize ?? 0.01 },
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
      updates.push(data => sma.setData(calculateSma(data, 20)))
    }
    if (indicators.has('ema')) {
      const ema = chart.addSeries(LineSeries, { color: '#79a7ff', lineWidth: 2, priceLineVisible: false, lastValueVisible: false })
      ema.setData(calculateEma(prices, 20))
      updates.push(data => ema.setData(calculateEma(data, 20)))
    }
    if (indicators.has('bollinger')) {
      const bands = calculateBollinger(prices, 20, 2)
      const upper = chart.addSeries(LineSeries, { color: '#c084fc', lineWidth: 1, lineStyle: LineStyle.Dashed, priceLineVisible: false, lastValueVisible: false })
      const lower = chart.addSeries(LineSeries, { color: '#c084fc', lineWidth: 1, lineStyle: LineStyle.Dashed, priceLineVisible: false, lastValueVisible: false })
      upper.setData(bands.upper)
      lower.setData(bands.lower)
      updates.push(data => { const next = calculateBollinger(data, 20, 2); upper.setData(next.upper); lower.setData(next.lower) })
    }
    if (indicators.has('volume')) {
      const volume = chart.addSeries(HistogramSeries, { priceScaleId: 'volume', priceFormat: { type: 'volume' }, priceLineVisible: false, lastValueVisible: false })
      volume.setData(prices.map((price) => ({ time: toTimestamp(price.time), value: price.volume, color: price.close >= price.open ? 'rgba(74, 222, 128, .36)' : 'rgba(255, 102, 143, .36)' })))
      chart.priceScale('volume').applyOptions({ scaleMargins: { top: 0.82, bottom: 0 } })
      updates.push(data => volume.setData(data.map(price => ({ time: toTimestamp(price.time), value: price.volume, color: price.close >= price.open ? 'rgba(74, 222, 128, .36)' : 'rgba(255, 102, 143, .36)' }))))
    }

    chartRef.current = chart
    seriesRef.current = series
    updateDataRef.current = data => {
      const range = chart.timeScale().getVisibleLogicalRange()
      if (style === 'candles') (series as ISeriesApi<'Candlestick'>).setData(data.map(price => ({ time: toTimestamp(price.time), open: price.open, high: price.high, low: price.low, close: price.close })))
      else (series as ISeriesApi<'Area'>).setData(data.map(price => ({ time: toTimestamp(price.time), value: price.close })))
      updates.forEach(update => update(data))
      if (range) chart.timeScale().setVisibleLogicalRange(range)
      else chart.timeScale().fitContent()
    }
    chart.timeScale().fitContent()
    if (savedRangeRef.current) chart.timeScale().setVisibleLogicalRange(savedRangeRef.current)
    else if (visibleRangeRef.current) chart.timeScale().setVisibleRange(visibleRangeRef.current)
    const rangeChanged = (range: IRange<Time> | null) => rangeCallbackRef.current?.(range)
    chart.timeScale().subscribeVisibleTimeRangeChange(rangeChanged)
    const crosshairMoved = (event: MouseEventParams<Time>) => {
      const bar = event.time ? pricesRef.current.find(price => toTimestamp(price.time) === event.time) ?? null : null
      setHoveredBar(bar)
    }
    chart.subscribeCrosshairMove(crosshairMoved)

    const resizeObserver = new ResizeObserver(() => chart.applyOptions({ width: container.clientWidth, height: container.clientHeight }))
    resizeObserver.observe(container)

    return () => {
      resizeObserver.disconnect()
      savedRangeRef.current = chart.timeScale().getVisibleLogicalRange()
      updateDataRef.current = null
      chart.timeScale().unsubscribeVisibleTimeRangeChange(rangeChanged)
      chart.unsubscribeCrosshairMove(crosshairMoved)
      riskLinesRef.current = []
      seriesRef.current = null
      chartRef.current = null
      chart.remove()
    }
  }, [indicators, style, instrument?.pricePrecision, instrument?.tickSize])

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
  }, [prices, style, indicators, instrument?.pricePrecision, instrument?.tickSize, takeProfit, stopLoss])

  function anchorPoint(point: Point): Point {
    const logical = chartRef.current?.timeScale().coordinateToLogical(point.x)
    const price = seriesRef.current?.coordinateToPrice(point.y)
    if (logical == null || price == null) return point
    if (magnet && prices.length) {
      const bar = prices[clamp(Math.round(logical), 0, prices.length - 1)]
      const nearest = [bar.open, bar.high, bar.low, bar.close].sort((a, b) => Math.abs(a - price) - Math.abs(b - price))[0]
      return { ...point, time: toTimestamp(bar.time), price: nearest }
    }
    return { ...point, time: timeAtLogical(prices, logical), price }
  }

  function projectPoint(point: Point): Point {
    if (point.time == null || point.price == null || !projection.barWidth) return point
    return { ...point, x: projection.x0 + logicalAtTime(prices, point.time) * projection.barWidth, y: projection.y0 + point.price * projection.priceScale }
  }

  function finishDrawing(drawing: OverlayDrawing) {
    setOverlayDrawings(drawings => [...drawings, { ...drawing, start: anchorPoint(drawing.start), end: anchorPoint(drawing.end), third: drawing.third ? anchorPoint(drawing.third) : undefined }])
    setDrawingStart(null)
    setDrawingSecond(null)
    setDrawingPreview(null)
    setTool('cursor')
    setSelectedDrawingId(drawing.id)
    workspaceRef.current?.focus({ preventScroll: true })
  }

  function clearDrawings() {
    setOverlayDrawings([])
    setDrawingStart(null)
    setDrawingSecond(null)
    setDrawingPreview(null)
    setSelectedDrawingId(null)
    setTool('cursor')
  }

  function drawOnOverlay(event: React.MouseEvent<SVGSVGElement>) {
    if (drawingsLocked || !overlayTools.has(tool)) return
    const bounds = event.currentTarget.getBoundingClientRect()
    const point = { x: event.clientX - bounds.left, y: event.clientY - bounds.top }
    if (point.x > plotSize.width || point.y > plotSize.height || point.x < 0 || point.y < 0) return

    if (['line', 'vertical-line', 'take-profit', 'stop-loss'].includes(tool)) {
      const drawing = createOverlayDrawing(tool as OverlayTool, point, point)
      finishDrawing(drawing)
      return
    }

    if (tool === 'text' || tool === 'callout') {
      const label = window.prompt(tool === 'callout' ? 'Enter callout text' : 'Enter chart note')?.trim()
      if (label) {
        const drawing = createOverlayDrawing(tool, point, point, label)
        finishDrawing(drawing)
      }
      if (!label) selectTool('cursor')
      return
    }

    if (!drawingStart) {
      setDrawingStart(anchorPoint(point))
      setDrawingPreview(anchorPoint(point))
      return
    }
    if (['parallel-channel', 'pitchfork', 'triangle'].includes(tool)) {
      if (!drawingSecond) { setDrawingSecond(anchorPoint(point)); return }
      finishDrawing({ ...createOverlayDrawing(tool as OverlayTool, projectPoint(drawingStart), projectPoint(drawingSecond)), third: point })
    } else finishDrawing(createOverlayDrawing(tool as OverlayTool, projectPoint(drawingStart), point))
  }

  function moveOnOverlay(event: ReactPointerEvent<SVGSVGElement>) {
    const point = pointInSvg(event.currentTarget, event.clientX, event.clientY)
    const drag = drawingDragRef.current
    if (!drag) {
      if (drawingStart) setDrawingPreview(anchorPoint(point))
      return
    }

    const delta = { x: point.x - drag.pointer.x, y: point.y - drag.pointer.y }
    setOverlayDrawings((drawings) => drawings.map((drawing) => {
      if (drawing.id !== drag.id) return drawing
      if (drag.mode === 'start') return { ...drawing, start: anchorPoint(addPoint(drag.original.start, delta)) }
      if (drag.mode === 'end') return { ...drawing, end: anchorPoint(addPoint(drag.original.end, delta)) }
      if (drag.mode === 'third' && drag.original.third) return { ...drawing, third: anchorPoint(addPoint(drag.original.third, delta)) }
      return {
        ...drawing,
        start: anchorPoint(addPoint(drag.original.start, delta)),
        end: anchorPoint(addPoint(drag.original.end, delta)),
        third: drag.original.third ? anchorPoint(addPoint(drag.original.third, delta)) : undefined,
      }
    }))
  }

  function startDrawingDrag(event: ReactPointerEvent<SVGGElement | SVGCircleElement>, drawing: OverlayDrawing, mode: DrawingDrag['mode']) {
    if (drawingsLocked || tool !== 'cursor') return
    const svg = event.currentTarget.ownerSVGElement
    if (!svg) return
    event.preventDefault()
    event.stopPropagation()
    drawingDragRef.current = {
      id: drawing.id,
      mode,
      pointer: pointInSvg(svg, event.clientX, event.clientY),
      original: { ...drawing, start: projectPoint(drawing.start), end: projectPoint(drawing.end), third: drawing.third ? projectPoint(drawing.third) : undefined },
    }
    setSelectedDrawingId(drawing.id)
    workspaceRef.current?.focus({ preventScroll: true })
    svg.setPointerCapture(event.pointerId)
  }

  function stopDrawingDrag(event: ReactPointerEvent<SVGSVGElement>) {
    drawingDragRef.current = null
    if (event.currentTarget.hasPointerCapture(event.pointerId)) {
      event.currentTarget.releasePointerCapture(event.pointerId)
    }
  }

  const drawingCount = overlayDrawings.length
  const legendBar = hoveredBar ?? prices.at(-1)

  function toggleIndicator(indicator: Indicator) {
    setIndicators((current) => {
      const next = new Set(current)
      if (next.has(indicator)) next.delete(indicator)
      else next.add(indicator)
      return next
    })
  }

  function selectTool(selectedTool: DrawingTool, groupId?: string) {
    setDrawingStart(null)
    setDrawingSecond(null)
    setDrawingPreview(null)
    setSelectedDrawingId(null)
    setTool(selectedTool)
    if (groupId) {
      setGroupSelections((current) => ({ ...current, [groupId]: selectedTool }))
      setOpenToolGroup(null)
    }
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
    <div ref={workspaceRef} className="trading-chart" tabIndex={0} onKeyDown={event => {
      if ((event.target as HTMLElement).closest('input, textarea, select, [contenteditable="true"]')) return
      if (event.key === 'Escape') { selectTool('cursor'); setOpenToolGroup(null); setIndicatorMenuOpen(false) }
      if ((event.key === 'Delete' || event.key === 'Backspace') && selectedDrawingId && !drawingsLocked) {
        event.preventDefault()
        setOverlayDrawings(drawings => drawings.filter(drawing => drawing.id !== selectedDrawingId))
        setSelectedDrawingId(null)
      }
    }} onPointerDownCapture={event => {
      if ((event.target as Element).closest('.chart-canvas')) { setSelectedDrawingId(null); setOpenToolGroup(null); setIndicatorMenuOpen(false); workspaceRef.current?.focus({ preventScroll: true }) }
    }}>
      <aside className="drawing-rail" aria-label="Drawing tools">
        {drawingToolGroups.map((group) => {
          const selectedTool = groupSelections[group.id]
          const selectedDefinition = drawingTools.find((item) => item.id === selectedTool)!
          const SelectedIcon = selectedDefinition.icon
          const isOpen = openToolGroup === group.id
          return <div key={group.id} className="drawing-tool-group">
            <ToolbarButton active={tool === selectedTool} label={selectedDefinition.label} onClick={() => selectTool(selectedTool)} icon={<SelectedIcon />} />
            <button type="button" className={`drawing-tool-group__arrow${isOpen ? ' is-open' : ''}`} aria-label={`${isOpen ? 'Close' : 'Show'} ${group.label}`} aria-expanded={isOpen} onClick={event => {
              const groupTop = event.currentTarget.parentElement!.getBoundingClientRect().top - workspaceRef.current!.getBoundingClientRect().top
              const menuHeight = Math.min(group.tools.length * 42 + 65, plotSize.height - 16)
              setToolMenuTop(clamp(groupTop, 8, Math.max(8, plotSize.height - menuHeight - 8)) - groupTop)
              setOpenToolGroup(isOpen ? null : group.id)
            }}><ChevronRight /></button>
            {isOpen && <div className="drawing-tool-menu" role="menu" aria-label={group.label} style={{ top: toolMenuTop, maxHeight: Math.max(100, plotSize.height - 16), overflowY: 'auto' }}>
              <strong>{group.label}</strong>
              {group.tools.map((groupTool) => {
                const definition = drawingTools.find((item) => item.id === groupTool)!
                const Icon = definition.icon
                const isFavorite = favoriteTools.includes(groupTool)
                return <div key={groupTool} className="drawing-tool-menu__row">
                  <button type="button" role="menuitem" className={tool === groupTool ? 'is-active' : ''} onClick={() => selectTool(groupTool, group.id)}><Icon /><span>{definition.label}</span></button>
                  {definition.favorite && <button type="button" className={`drawing-menu-favorite${isFavorite ? ' is-favorite' : ''}`} aria-label={`${isFavorite ? 'Remove' : 'Add'} ${definition.label} ${isFavorite ? 'from' : 'to'} favorites`} aria-pressed={isFavorite} title={`${isFavorite ? 'Remove from' : 'Add to'} favorites`} onClick={() => toggleFavorite(groupTool)}><Star /></button>}
                </div>
              })}
            </div>}
          </div>
        })}
        <span className="drawing-rail__separator" />
        <ToolbarButton active={style === 'candles'} label="Candles" onClick={() => setStyle('candles')} icon={<CandlestickChart />} />
        <ToolbarButton active={style === 'area'} label="Area chart" onClick={() => setStyle('area')} icon={<AreaChart />} />
        <ToolbarButton active={indicatorMenuOpen} label="Indicators" onClick={() => setIndicatorMenuOpen((value) => !value)} icon={<ChartSpline />} />
        <ToolbarButton label="Fit chart" onClick={() => chartRef.current?.timeScale().fitContent()} icon={<Maximize />} />
        <ToolbarButton active={magnet} label="Snap to candle OHLC" onClick={() => setMagnet(value => !value)} icon={<Magnet />} />
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
      {overlayTools.has(tool) && <p className="trading-chart__hint">{drawingStart ? drawingSecond ? 'Click the third anchor to finish.' : ['parallel-channel', 'pitchfork', 'triangle'].includes(tool) ? 'Click the second anchor, then a third anchor.' : 'Move the pointer, then click to finish.' : `Click the chart to start a ${toolLabels[tool].toLowerCase()}.`}</p>}
      <div ref={containerRef} aria-label="Interactive historical price chart" className="chart-canvas" />
      {legendBar && <div className="chart-candle-legend" aria-label="Candle OHLC"><span>{new Date(legendBar.time).toISOString().slice(0, 16).replace('T', ' ')} UTC</span>{(['open', 'high', 'low', 'close'] as const).map(field => <span key={field}>{field[0].toUpperCase()} <b>{legendBar[field].toFixed(instrument?.pricePrecision ?? 2)}</b></span>)}</div>}
      <svg className={`chart-drawing-overlay${overlayTools.has(tool) ? ' is-drawing' : ''}`} onClick={drawOnOverlay} onPointerMove={moveOnOverlay} onPointerUp={stopDrawingDrag} onPointerCancel={stopDrawingDrag} aria-hidden="true">
        <defs>
          <clipPath id={clipId}><rect width={plotSize.width} height={plotSize.height} /></clipPath>
          <marker id="chart-arrow" viewBox="0 0 10 10" refX="9" refY="5" markerWidth="6" markerHeight="6" orient="auto-start-reverse">
            <path d="M 0 0 L 10 5 L 0 10 z" />
          </marker>
        </defs>
        <g clipPath={`url(#${clipId})`}>
        {drawingsVisible && overlayDrawings.map((stored) => {
          const drawing = { ...stored, start: projectPoint(stored.start), end: projectPoint(stored.end), third: stored.third ? projectPoint(stored.third) : undefined }
          const selected = selectedDrawingId === drawing.id
          return <g key={drawing.id} data-start-time={drawing.start.time} data-start-price={drawing.start.price} className={`chart-drawing-object${selected ? ' is-selected' : ''}`} onClick={(event) => event.stopPropagation()} onPointerDown={(event) => startDrawingDrag(event, stored, 'move')}>
            <g className="drawing-hit-area"><OverlayShape drawing={drawing} /></g>
            <OverlayShape drawing={drawing} />
            {selected && !drawingsLocked && <>
              <circle className="drawing-handle" cx={drawing.start.x} cy={drawing.start.y} r="6" onPointerDown={(event) => startDrawingDrag(event, drawing, 'start')} />
              {!isSinglePointTool(drawing.type) && <circle className="drawing-handle" cx={drawing.end.x} cy={drawing.end.y} r="6" onPointerDown={(event) => startDrawingDrag(event, drawing, 'end')} />}
              {drawing.third && <circle className="drawing-handle" cx={drawing.third.x} cy={drawing.third.y} r="6" onPointerDown={(event) => startDrawingDrag(event, drawing, 'third')} />}
            </>}
          </g>
        })}
        {drawingStart && drawingPreview && <g className="drawing-preview"><OverlayShape drawing={{ ...createOverlayDrawing(tool as OverlayTool, projectPoint(drawingStart), projectPoint(drawingSecond ?? drawingPreview), undefined, 'preview'), third: drawingSecond ? projectPoint(drawingPreview) : undefined }} /></g>}
        {drawingStart && <circle className="drawing-anchor" cx={projectPoint(drawingStart).x} cy={projectPoint(drawingStart).y} r="5" />}
        </g>
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
    case 'extended-line':
      return <line className="drawing-extended" x1={drawing.start.x - (drawing.end.x - drawing.start.x) * 8} y1={drawing.start.y - (drawing.end.y - drawing.start.y) * 8} x2={drawing.end.x + (drawing.end.x - drawing.start.x) * 8} y2={drawing.end.y + (drawing.end.y - drawing.start.y) * 8} />
    case 'arrow':
      return <line className="drawing-arrow" x1={drawing.start.x} y1={drawing.start.y} x2={drawing.end.x} y2={drawing.end.y} markerEnd="url(#chart-arrow)" />
    case 'line':
      return <g className="drawing-horizontal"><line x1="0" y1={drawing.start.y} x2="100%" y2={drawing.start.y} /><text x={drawing.start.x + 8} y={drawing.start.y - 6}>LINE</text></g>
    case 'vertical-line':
      return <line className="drawing-vertical" x1={drawing.start.x} y1="0" x2={drawing.start.x} y2="100%" />
    case 'take-profit':
    case 'stop-loss':
      return <g className={`drawing-risk-level ${drawing.type === 'take-profit' ? 'is-profit' : 'is-loss'}`}><line x1="0" y1={drawing.start.y} x2="100%" y2={drawing.start.y} /><text x={drawing.start.x + 8} y={drawing.start.y - 6}>{drawing.type === 'take-profit' ? 'TAKE PROFIT' : 'STOP LOSS'}</text></g>
    case 'rectangle':
      return <rect x={left} y={top} width={width} height={height} />
    case 'ellipse':
      return <ellipse cx={left + width / 2} cy={top + height / 2} rx={width / 2} ry={height / 2} />
    case 'triangle':
      return <polygon className="triangle-drawing" points={`${drawing.start.x},${drawing.start.y} ${drawing.end.x},${drawing.end.y} ${drawing.third?.x ?? left},${drawing.third?.y ?? top + height}`} />
    case 'parallel-channel':
      return <g className="parallel-channel"><line x1={drawing.start.x} y1={drawing.start.y} x2={drawing.end.x} y2={drawing.end.y} /><line x1={drawing.third?.x ?? drawing.start.x} y1={drawing.third?.y ?? drawing.start.y + 24} x2={drawing.end.x + (drawing.third ? drawing.third.x - drawing.start.x : 0)} y2={drawing.end.y + (drawing.third ? drawing.third.y - drawing.start.y : 24)} /></g>
    case 'pitchfork':
      if (drawing.third) {
        const midpoint = { x: (drawing.end.x + drawing.third.x) / 2, y: (drawing.end.y + drawing.third.y) / 2 }
        const delta = { x: (midpoint.x - drawing.start.x) * 20, y: (midpoint.y - drawing.start.y) * 20 }
        return <g className="pitchfork-drawing">{[drawing.start, drawing.end, drawing.third].map((point, index) => <line key={index} x1={point.x} y1={point.y} x2={point.x + delta.x} y2={point.y + delta.y} />)}</g>
      }
      return <g className="pitchfork-drawing"><line x1={drawing.start.x} y1={drawing.start.y} x2={drawing.end.x} y2={drawing.end.y} /><line x1={drawing.start.x} y1={drawing.start.y} x2={drawing.end.x} y2={drawing.end.y - 34} /><line x1={drawing.start.x} y1={drawing.start.y} x2={drawing.end.x} y2={drawing.end.y + 34} /></g>
    case 'fib':
      return <FibonacciDrawing drawing={drawing} />
    case 'price-range':
      return <g className="price-range-drawing"><rect x={left} y={top} width={width} height={height} /><line x1={left} y1={top} x2={left + width} y2={top + height} /><text x={left + 6} y={top + 16}>{drawing.start.price != null && drawing.end.price != null ? `${(drawing.end.price - drawing.start.price).toFixed(4)} (${((drawing.end.price / drawing.start.price - 1) * 100).toFixed(2)}%)` : 'PRICE RANGE'}</text></g>
    case 'date-range':
      return <g className="date-range-drawing"><rect x={left} y={top} width={width} height={height} /><line x1={left} y1={top} x2={left} y2={top + height} /><line x1={left + width} y1={top} x2={left + width} y2={top + height} /><text x={left + 6} y={top + 16}>{drawing.start.time != null && drawing.end.time != null ? `${(Math.abs(drawing.end.time - drawing.start.time) / 3600).toFixed(1)} hours` : 'DATE RANGE'}</text></g>
    case 'long-position':
    case 'short-position':
      return <PositionDrawing drawing={drawing} />
    case 'text':
      return <g className="text-drawing"><rect x={drawing.start.x - 4} y={drawing.start.y - 18} width={Math.max(52, (drawing.label?.length ?? 0) * 7 + 12)} height="25" /><text x={drawing.start.x + 3} y={drawing.start.y}>{drawing.label}</text></g>
    case 'callout':
      return <g className="callout-drawing"><line x1={drawing.start.x} y1={drawing.start.y} x2={drawing.start.x + 28} y2={drawing.start.y - 28} /><rect x={drawing.start.x + 24} y={drawing.start.y - 52} width={Math.max(64, (drawing.label?.length ?? 0) * 7 + 14)} height="28" /><text x={drawing.start.x + 31} y={drawing.start.y - 34}>{drawing.label}</text></g>
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

function createInitialGroupSelections() {
  return Object.fromEntries(drawingToolGroups.map((group) => [group.id, group.tools[0]])) as Record<string, DrawingTool>
}

function createOverlayDrawing(type: OverlayTool, start: Point, end: Point, label?: string, id = `drawing-${Date.now()}-${Math.random().toString(36).slice(2)}`): OverlayDrawing {
  return { id, type, start, end, label }
}

function pointInSvg(svg: SVGSVGElement, clientX: number, clientY: number): Point {
  const bounds = svg.getBoundingClientRect()
  return { x: clientX - bounds.left, y: clientY - bounds.top }
}

function addPoint(point: Point, delta: Point): Point {
  return { x: point.x + delta.x, y: point.y + delta.y }
}

function timeAtLogical(prices: HistoricalPrice[], logical: number): number {
  if (!prices.length) return 0
  if (prices.length === 1) return toTimestamp(prices[0].time) + logical * 60
  const index = clamp(Math.floor(logical), 0, prices.length - 2)
  const start = toTimestamp(prices[index].time)
  return start + (logical - index) * (toTimestamp(prices[index + 1].time) - start)
}

function logicalAtTime(prices: HistoricalPrice[], time: number): number {
  if (!prices.length) return 0
  if (prices.length === 1) return (time - toTimestamp(prices[0].time)) / 60
  let low = 0
  let high = prices.length - 1
  while (low < high) { const mid = Math.floor((low + high) / 2); if (toTimestamp(prices[mid].time) < time) low = mid + 1; else high = mid }
  const index = clamp(low - 1, 0, prices.length - 2)
  const start = toTimestamp(prices[index].time)
  return index + (time - start) / (toTimestamp(prices[index + 1].time) - start)
}

function isSinglePointTool(tool: OverlayTool) {
  return ['line', 'vertical-line', 'text', 'callout', 'take-profit', 'stop-loss'].includes(tool)
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
