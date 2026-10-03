export function formatMoney(value: number) {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
  }).format(value)
}

export function formatQuantity(value: number) {
  return new Intl.NumberFormat('en-US', {
    maximumFractionDigits: 6,
  }).format(value)
}
