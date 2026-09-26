import { Navigate, Route, Routes } from 'react-router'
import { GuestRoute } from './features/auth/guest-route'
import { ProtectedRoute } from './features/auth/protected-route'
import { DashboardPage } from './pages/dashboard-page'
import { LoginPage } from './pages/login-page'
import { RegisterPage } from './pages/register-page'

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
      </Route>

      <Route
        path="*"
        element={<Navigate to="/dashboard" replace />}
      />
    </Routes>
  )
}

export default App