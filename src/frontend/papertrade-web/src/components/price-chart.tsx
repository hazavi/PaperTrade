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
        background: { type: ColorType.Solid, color: 'transparent' },
        textColor: '#9fb3ca',
      },
      grid: {
        vertLines: { color: 'rgba(255, 255, 255, 0.055)' },
        horzLines: { color: 'rgba(255, 255, 255, 0.055)' },
      },
      rightPriceScale: { borderColor: 'rgba(255, 255, 255, 0.14)' },
      timeScale: { borderColor: 'rgba(255, 255, 255, 0.14)', timeVisible: true },
    })

    const series: ISeriesApi<'Area'> = chart.addSeries(AreaSeries, {
      lineColor: '#7ddcff',
      topColor: 'rgba(125, 220, 255, 0.32)',
      bottomColor: 'rgba(128, 143, 255, 0.015)',
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
