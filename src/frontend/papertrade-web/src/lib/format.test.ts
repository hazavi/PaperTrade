import { describe, expect, it } from 'vitest'
import { formatMoney, formatQuantity } from './format'

describe('trading formatters', () => {
  it('formats money with two decimal places', () => {
    expect(formatMoney(1948.2)).toBe('$1,948.20')
  })

  it('keeps fractional share precision without trailing zeroes', () => {
    expect(formatQuantity(2.500001)).toBe('2.500001')
    expect(formatQuantity(10)).toBe('10')
  })
})
