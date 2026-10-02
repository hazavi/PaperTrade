import {
  AreaSeries,
  ColorType,
  createChart,
  type IChartApi,
  type ISeriesApi,
  type UTCTimestamp,
} from 'lightweight-charts'
import { useEffect, useRef } from 'react'
import type { HistoricalPrice } from '../features/markets/market-types'

export function PriceChart({ prices }: { prices: HistoricalPrice[] }) {
  const containerRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    const container = containerRef.current
    if (!container) return

    const chart: IChartApi = createChart(container, {
      height: 360,
      layout: {
        background: { type: ColorType.Solid, color: '#0f172a' },
        textColor: '#94a3b8',
      },
      grid: {
        vertLines: { color: '#1e293b' },
        horzLines: { color: '#1e293b' },
      },
      rightPriceScale: { borderColor: '#334155' },
      timeScale: { borderColor: '#334155', timeVisible: true },
    })

    const series: ISeriesApi<'Area'> = chart.addSeries(AreaSeries, {
      lineColor: '#34d399',
      topColor: 'rgba(52, 211, 153, 0.35)',
      bottomColor: 'rgba(52, 211, 153, 0.02)',
      priceLineVisible: false,
    })

    series.setData(
      prices.map((price) => ({
        time: Math.floor(new Date(price.time).getTime() / 1000) as UTCTimestamp,
        value: price.close,
      })),
    )
    chart.timeScale().fitContent()

    const resizeObserver = new ResizeObserver(() => {
      chart.applyOptions({ width: container.clientWidth })
    })
    resizeObserver.observe(container)

    return () => {
      resizeObserver.disconnect()
      chart.remove()
    }
  }, [prices])

  return <div ref={containerRef} aria-label="Historical price chart" />
}
