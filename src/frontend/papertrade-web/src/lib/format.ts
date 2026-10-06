import type { Instrument } from '../features/markets/market-types'

export function formatMoney(value: number) {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
  }).format(value)
}

export function formatPrice(value: number, instrument?: Pick<Instrument, 'pricePrecision' | 'tickSize' | 'quoteCurrency'> | null) {
  const precision = instrument?.pricePrecision ?? 2
  const tickSize = instrument?.tickSize ?? 0.01
  const rounded = Math.round(value / tickSize) * tickSize
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: instrument?.quoteCurrency ?? 'USD',
    minimumFractionDigits: precision,
    maximumFractionDigits: precision,
  }).format(rounded)
}

export function formatInstrumentQuantity(value: number, instrument?: Pick<Instrument, 'quantityPrecision'> | null) {
  return new Intl.NumberFormat('en-US', {
    maximumFractionDigits: instrument?.quantityPrecision ?? 6,
  }).format(value)
}

export function formatQuantity(value: number) {
  return new Intl.NumberFormat('en-US', {
    maximumFractionDigits: 6,
  }).format(value)
}
