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
    <nav aria-label="Main navigation" className="glass-nav">
      <div className="glass-nav__scroll">
        {links.map(([to, label]) => (
          <NavLink
            key={to}
            to={to}
            className={({ isActive }) =>
              `glass-nav__link${isActive ? ' glass-nav__link--active' : ''}`
            }
          >
            {label}
          </NavLink>
        ))}
      </div>
    </nav>
  )
}
