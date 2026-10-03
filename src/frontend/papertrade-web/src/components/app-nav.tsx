import { NavLink } from 'react-router'

const links = [
  ['/dashboard', 'Dashboard'],
  ['/markets', 'Markets'],
  ['/portfolio', 'Portfolio'],
  ['/orders', 'Orders'],
  ['/watchlists', 'Watchlists'],
  ['/alerts', 'Alerts'],
  ['/notifications', 'Notifications'],
  ['/leaderboard', 'Leaderboard'],
] as const

export function AppNav() {
  return (
    <nav aria-label="Main navigation" className="flex flex-wrap gap-2">
      {links.map(([to, label]) => (
        <NavLink
          key={to}
          to={to}
          className={({ isActive }) =>
            `rounded-lg px-3 py-2 text-sm font-medium transition ${
              isActive
                ? 'bg-emerald-400 text-slate-950'
                : 'text-slate-300 hover:bg-slate-800 hover:text-white'
            }`
          }
        >
          {label}
        </NavLink>
      ))}
    </nav>
  )
}
