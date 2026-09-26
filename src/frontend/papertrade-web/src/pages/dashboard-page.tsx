import { useCurrentUser } from '../features/auth/auth-queries'

export function DashboardPage() {
  const { data: user } = useCurrentUser()

  if (!user) {
    return null
  }

  return (
    <main className="min-h-screen px-6 py-10">
      <div className="mx-auto max-w-6xl">
        <header>
          <p className="text-sm font-medium uppercase tracking-widest text-emerald-400">
            PaperTrade
          </p>
          <h1 className="mt-2 text-3xl font-bold text-white">
            Welcome, {user.displayName}
          </h1>
          <p className="mt-2 text-slate-400">{user.email}</p>
        </header>

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