import { Navigate, Route, Routes } from 'react-router'
import { lazy, Suspense, type ReactNode } from 'react'
import { FullPageStatus } from './components/full-page-status'
import { GuestRoute } from './features/auth/guest-route'
import { ProtectedRoute } from './features/auth/protected-route'
import { DashboardPage } from './pages/dashboard-page'
import { LoginPage } from './pages/login-page'
import { RegisterPage } from './pages/register-page'

const MarketsPage = lazy(() =>
  import('./pages/markets-page').then((module) => ({
    default: module.MarketsPage,
  })),
)
const MarketDetailPage = lazy(() =>
  import('./pages/market-detail-page').then((module) => ({
    default: module.MarketDetailPage,
  })),
)
const WatchlistsPage = lazy(() =>
  import('./pages/watchlists-page').then((module) => ({
    default: module.WatchlistsPage,
  })),
)
const PortfolioPage = lazy(() =>
  import('./pages/portfolio-page').then((module) => ({ default: module.PortfolioPage })),
)
const OrdersPage = lazy(() =>
  import('./pages/orders-page').then((module) => ({ default: module.OrdersPage })),
)
const AlertsPage = lazy(() => import('./pages/alerts-page').then((module) => ({ default: module.AlertsPage })))
const NotificationsPage = lazy(() => import('./pages/notifications-page').then((module) => ({ default: module.NotificationsPage })))
const LeaderboardPage = lazy(() => import('./pages/leaderboard-page').then((module) => ({ default: module.LeaderboardPage })))

function App() {
  return (
    <Routes>
      <Route
        path="/"
        element={<Navigate to="/dashboard" replace />}
      />

      <Route element={<GuestRoute />}>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />
      </Route>

      <Route element={<ProtectedRoute />}>
        <Route path="/dashboard" element={<DashboardPage />} />
        <Route path="/markets" element={<LazyPage><MarketsPage /></LazyPage>} />
        <Route path="/markets/:symbol" element={<LazyPage><MarketDetailPage /></LazyPage>} />
        <Route path="/watchlists" element={<LazyPage><WatchlistsPage /></LazyPage>} />
        <Route path="/portfolio" element={<LazyPage><PortfolioPage /></LazyPage>} />
        <Route path="/orders" element={<LazyPage><OrdersPage /></LazyPage>} />
        <Route path="/alerts" element={<LazyPage><AlertsPage /></LazyPage>} />
        <Route path="/notifications" element={<LazyPage><NotificationsPage /></LazyPage>} />
        <Route path="/leaderboard" element={<LazyPage><LeaderboardPage /></LazyPage>} />
      </Route>

      <Route
        path="*"
        element={<Navigate to="/dashboard" replace />}
      />
    </Routes>
  )
}

function LazyPage({ children }: { children: ReactNode }) {
  return (
    <Suspense fallback={<FullPageStatus title="Loading" message="Preparing the page..." />}>
      {children}
    </Suspense>
  )
}

export default App
