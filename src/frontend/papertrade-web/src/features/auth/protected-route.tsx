import { Navigate, Outlet, useLocation } from 'react-router'
import { FullPageStatus } from '../../components/full-page-status'
import { useRealtimeNotifications } from '../realtime/use-realtime-symbol'
import { useCurrentUser } from './auth-queries'

export function ProtectedRoute() {
  const location = useLocation()
  const currentUser = useCurrentUser()
  useRealtimeNotifications(Boolean(currentUser.data))

  if (currentUser.isPending) {
    return (
      <FullPageStatus
        title="Loading PaperTrade"
        message="Checking your session..."
      />
    )
  }

  if (currentUser.isError) {
    return (
      <FullPageStatus
        title="Unable to load your session"
        message="Check that the API is running, then try again."
        action={
          <button
            type="button"
            disabled={currentUser.isFetching}
            onClick={() => void currentUser.refetch()}
            className="rounded-lg bg-emerald-500 px-4 py-2 font-semibold text-slate-950 disabled:opacity-50"
          >
            {currentUser.isFetching ? 'Trying again...' : 'Try again'}
          </button>
        }
      />
    )
  }

  if (!currentUser.data) {
    return (
      <Navigate
        to="/login"
        replace
        state={{ from: location.pathname }}
      />
    )
  }

  return <Outlet />
}
