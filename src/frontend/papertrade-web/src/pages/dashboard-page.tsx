import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useNavigate } from 'react-router'
import { logout } from '../features/auth/auth-api'
import {
  authKeys,
  useCurrentUser,
} from '../features/auth/auth-queries'
import { ApiError } from '../lib/api-client'
import { AppNav } from '../components/app-nav'

export function DashboardPage() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const { data: user } = useCurrentUser()

  const logoutMutation = useMutation({
    mutationFn: logout,
    onSuccess: () => {
      queryClient.setQueryData(authKeys.currentUser, null)
      navigate('/login', { replace: true })
    },
  })

  if (!user) {
    return null
  }

  const logoutError =
    logoutMutation.error instanceof ApiError
      ? logoutMutation.error.message
      : logoutMutation.isError
        ? 'Unable to log out.'
        : null

  return (
    <main className="min-h-screen px-6 py-10">
      <div className="mx-auto max-w-6xl">
        <header className="flex flex-col gap-5 sm:flex-row sm:items-start sm:justify-between">
          <div>
            <p className="text-sm font-medium uppercase tracking-widest text-emerald-400">
              PaperTrade
            </p>
            <h1 className="mt-2 text-3xl font-bold text-white">
              Welcome, {user.displayName}
            </h1>
            <p className="mt-2 text-slate-400">{user.email}</p>
          </div>

          <div className="flex flex-col items-start gap-3 sm:items-end">
            <AppNav />
            <button
              type="button"
              disabled={logoutMutation.isPending}
              onClick={() => logoutMutation.mutate()}
              className="rounded-lg border border-slate-700 px-4 py-2 text-sm font-medium text-slate-200 transition hover:border-slate-500 hover:text-white disabled:cursor-not-allowed disabled:opacity-50"
            >
              {logoutMutation.isPending ? 'Logging out...' : 'Log out'}
            </button>
          </div>
        </header>

        {logoutError && (
          <p
            role="alert"
            className="mt-6 rounded-lg border border-red-900 bg-red-950/50 p-3 text-sm text-red-300"
          >
            {logoutError}
          </p>
        )}

        <section className="mt-10 grid gap-4 md:grid-cols-3">
          <DashboardCard
            label="Portfolio value"
            value="$100,000.00"
          />
          <DashboardCard
            label="Available cash"
            value="$100,000.00"
          />
          <DashboardCard label="Today's P&L" value="$0.00" />
        </section>

        <section className="mt-8 rounded-2xl border border-slate-800 bg-slate-900 p-6">
          <h2 className="text-xl font-semibold text-white">
            Positions
          </h2>
          <p className="mt-3 text-slate-400">No positions yet.</p>
        </section>
      </div>
    </main>
  )
}

type DashboardCardProps = {
  label: string
  value: string
}

function DashboardCard({
  label,
  value,
}: DashboardCardProps) {
  return (
    <article className="rounded-2xl border border-slate-800 bg-slate-900 p-6">
      <p className="text-sm text-slate-400">{label}</p>
      <p className="mt-2 text-2xl font-semibold text-white">
        {value}
      </p>
    </article>
  )
}
