import { useState } from 'react'
import { AppNav } from '../components/app-nav'
import { Brand } from '../components/brand'
import { useLeaderboard } from '../features/leaderboard/leaderboard-queries'
import { formatMoney } from '../lib/format'

export function LeaderboardPage() {
  const [page, setPage] = useState(1)
  const leaderboard = useLeaderboard(page)
  const hasNext = leaderboard.data ? page * leaderboard.data.pageSize < leaderboard.data.totalCount : false
  return <main className="min-h-screen px-6 py-8"><div className="mx-auto max-w-6xl">
    <header className="flex flex-col gap-5 sm:flex-row sm:items-center sm:justify-between"><div><Brand /><h1 className="mt-2 text-3xl font-bold text-white">Leaderboard</h1><p className="mt-2 text-slate-400">Ranked by return from the same $100,000 starting balance.</p></div><AppNav /></header>
    <section className="mt-10 overflow-hidden rounded-2xl border border-slate-800 bg-slate-900">
      {leaderboard.isLoading && <p className="p-5 text-slate-400">Calculating rankings...</p>}
      {leaderboard.data?.entries.map((entry) => <div key={entry.userId} className="grid grid-cols-[4rem_1fr_auto] items-center gap-4 border-b border-slate-800 p-5 last:border-0"><p className="text-2xl font-bold text-slate-500">#{entry.rank}</p><div><p className="font-semibold text-white">{entry.displayName}</p><p className="text-sm text-slate-500">{formatMoney(entry.portfolioValue)}</p></div><p className={`font-semibold ${entry.returnPercentage >= 0 ? 'text-emerald-400' : 'text-red-400'}`}>{entry.returnPercentage >= 0 ? '+' : ''}{entry.returnPercentage.toFixed(2)}%</p></div>)}
    </section>
    <div className="mt-5 flex justify-end gap-3"><button type="button" disabled={page === 1} onClick={() => setPage((value) => value - 1)} className="rounded-lg border border-slate-700 px-4 py-2 text-slate-300 disabled:opacity-40">Previous</button><button type="button" disabled={!hasNext} onClick={() => setPage((value) => value + 1)} className="rounded-lg border border-slate-700 px-4 py-2 text-slate-300 disabled:opacity-40">Next</button></div>
  </div></main>
}
