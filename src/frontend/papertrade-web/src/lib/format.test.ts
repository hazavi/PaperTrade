import { describe, expect, it } from 'vitest'
import { formatMoney, formatPrice, formatQuantity, formatInstrumentQuantity } from './format'

describe('trading formatters', () => {
  it('formats money with two decimal places', () => {
    expect(formatMoney(1948.2)).toBe('$1,948.20')
  })

  it('keeps fractional share precision without trailing zeroes', () => {
    expect(formatQuantity(2.500001)).toBe('2.500001')
    expect(formatQuantity(10)).toBe('10')
  })

  it('uses instrument tick and precision for prices and quantities', () => {
    const instrument = { pricePrecision: 4, quantityPrecision: 2, tickSize: 0.0005, quoteCurrency: 'USD' }
    expect(formatPrice(1.23426, instrument)).toBe('$1.2345')
    expect(formatInstrumentQuantity(12.345, instrument)).toBe('12.35')
  })
})
