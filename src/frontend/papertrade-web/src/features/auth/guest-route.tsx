import { Navigate, Outlet } from 'react-router'
import { FullPageStatus } from '../../components/full-page-status'
import { useCurrentUser } from './auth-queries'

export function GuestRoute() {
  const currentUser = useCurrentUser()

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

  if (currentUser.data) {
    return <Navigate to="/dashboard" replace />
  }

  return <Outlet />
}