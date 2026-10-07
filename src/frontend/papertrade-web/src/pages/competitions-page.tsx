import { useState, type FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { AppNav } from '../components/app-nav'
import { Brand } from '../components/brand'
import { apiRequest } from '../lib/api-client'

type Competition = { id: string; name: string; joinCode: string; startsAt: string; endsAt: string }
type Entry = { userId: string; displayName: string; startingEquity: number | null; currentEquity: number | null; returnPercent: number | null }
type Standings = { entries: Entry[] }

export function CompetitionsPage() {
  const client = useQueryClient()
  const [name, setName] = useState('')
  const [startsAt, setStartsAt] = useState('')
  const [endsAt, setEndsAt] = useState('')
  const [code, setCode] = useState('')
  const [selected, setSelected] = useState('')
  const competitions = useQuery({ queryKey: ['competitions'], queryFn: () => apiRequest<Competition[]>('/api/competitions') })
  const standings = useQuery({ queryKey: ['competitions', selected, 'leaderboard'], queryFn: () => apiRequest<Standings>(`/api/competitions/${selected}/leaderboard`), enabled: !!selected, refetchInterval: 30000 })
  const create = useMutation({ mutationFn: () => apiRequest<Competition>('/api/competitions', { method: 'POST', body: JSON.stringify({ name, startsAt: new Date(startsAt).toISOString(), endsAt: new Date(endsAt).toISOString() }) }), onSuccess: value => { setSelected(value.id); setName(''); client.invalidateQueries({ queryKey: ['competitions'] }) } })
  const join = useMutation({ mutationFn: () => apiRequest<{ id: string }>('/api/competitions/join', { method: 'POST', body: JSON.stringify({ code }) }), onSuccess: value => { setSelected(value.id); setCode(''); client.invalidateQueries({ queryKey: ['competitions'] }) } })
  const active = competitions.data?.find(x => x.id === selected)
  function submitCreate(event: FormEvent) { event.preventDefault(); create.mutate() }
  function submitJoin(event: FormEvent) { event.preventDefault(); join.mutate() }
  return <main className="min-h-screen px-6 py-8"><div className="mx-auto max-w-6xl">
    <header className="flex flex-col gap-5 sm:flex-row sm:items-center sm:justify-between"><div><Brand /><h1 className="mt-2 text-3xl font-bold text-white">Competitions</h1><p className="mt-2 text-slate-400">Private paper trading contests ranked by return from each member’s equity at the shared start time.</p></div><AppNav /></header>
    <div className="mt-8 grid gap-4 md:grid-cols-2">
      <form onSubmit={submitCreate} className="grid gap-3 rounded-2xl border border-slate-800 bg-slate-900 p-5"><h2 className="font-semibold">Create competition</h2><input required maxLength={100} value={name} onChange={e => setName(e.target.value)} placeholder="Name" className="rounded bg-slate-950 p-3" /><label>Starts <input required type="datetime-local" value={startsAt} onChange={e => setStartsAt(e.target.value)} className="ml-2 rounded bg-slate-950 p-2" /></label><label>Ends <input required type="datetime-local" value={endsAt} onChange={e => setEndsAt(e.target.value)} className="ml-2 rounded bg-slate-950 p-2" /></label><button disabled={create.isPending} className="rounded bg-emerald-400 p-2 font-semibold text-slate-950">Create</button>{create.isError && <p role="alert" className="text-red-300">Could not create competition. Start at least five minutes ahead; end within 90 days.</p>}</form>
      <form onSubmit={submitJoin} className="grid content-start gap-3 rounded-2xl border border-slate-800 bg-slate-900 p-5"><h2 className="font-semibold">Join with code</h2><input required value={code} onChange={e => setCode(e.target.value.toUpperCase())} placeholder="Invite code" className="rounded bg-slate-950 p-3 uppercase" /><button disabled={join.isPending} className="rounded bg-emerald-400 p-2 font-semibold text-slate-950">Join</button>{join.isError && <p role="alert" className="text-red-300">Code not found or registration is closed.</p>}</form>
    </div>
    <section className="mt-8 rounded-2xl border border-slate-800 bg-slate-900 p-5"><h2 className="font-semibold">My competitions</h2><div className="mt-3 flex flex-wrap gap-2">{competitions.data?.map(item => <button key={item.id} onClick={() => setSelected(item.id)} className={`rounded border px-3 py-2 ${selected === item.id ? 'border-emerald-400' : 'border-slate-700'}`}>{item.name}</button>)}</div>{competitions.data?.length === 0 && <p className="mt-3 text-slate-400">Create one or join with a code.</p>}</section>
    {active && <section className="mt-5 rounded-2xl border border-slate-800 bg-slate-900 p-5"><h2 className="text-xl font-semibold">{active.name}</h2><p className="mt-2 text-slate-400">Invite code: <strong className="text-white">{active.joinCode}</strong> · {new Date(active.startsAt).toLocaleString()} to {new Date(active.endsAt).toLocaleString()}</p><div className="mt-4">{standings.data?.entries.map((entry, index) => <div key={entry.userId} className="flex justify-between border-t border-slate-800 py-3"><span>#{index + 1} {entry.displayName}</span><span>{entry.returnPercent == null ? 'Waiting for start equity or market quotes' : `${entry.returnPercent >= 0 ? '+' : ''}${entry.returnPercent.toFixed(2)}%`}</span></div>)}</div></section>}
  </div></main>
}
