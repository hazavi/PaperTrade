import { Link } from 'react-router'

type BrandProps = {
  className?: string
  compact?: boolean
  to?: string
}

export function Brand({
  className = '',
  compact = false,
  to = '/dashboard',
}: BrandProps) {
  return (
    <Link
      to={to}
      className={`app-brand${compact ? ' app-brand--compact' : ''}${className ? ` ${className}` : ''}`}
      aria-label={compact ? 'PaperTrade dashboard' : undefined}
    >
      <img src="/logo.png" alt="" width="40" height="40" />
      {!compact && <span>PaperTrade</span>}
    </Link>
  )
}
