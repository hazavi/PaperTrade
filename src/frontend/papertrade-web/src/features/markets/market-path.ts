export function marketDetailPath(symbol: string) {
  const parts = symbol.split('/')
  return parts.length === 2
    ? `/markets/pair/${encodeURIComponent(parts[0])}/${encodeURIComponent(parts[1])}`
    : `/markets/${encodeURIComponent(symbol)}`
}
