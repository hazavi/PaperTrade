import { AppNav } from '../components/app-nav'
import { useMarkNotificationRead, useNotifications } from '../features/engagement/engagement-queries'

export function NotificationsPage() {
  const notifications = useNotifications()
  const markRead = useMarkNotificationRead()
  return <main className="min-h-screen px-6 py-8"><div className="mx-auto max-w-6xl">
    <header className="flex flex-col gap-5 sm:flex-row sm:items-center sm:justify-between"><div><p className="text-sm font-medium uppercase tracking-widest text-emerald-400">PaperTrade</p><h1 className="mt-2 text-3xl font-bold text-white">Notifications</h1></div><AppNav /></header>
    <section className="mt-10 overflow-hidden rounded-2xl border border-slate-800 bg-slate-900">
      {notifications.isLoading && <p className="p-5 text-slate-400">Loading notifications...</p>}
      {notifications.data?.length === 0 && <p className="p-5 text-slate-400">No notifications yet.</p>}
      {notifications.data?.map((notification) => <article key={notification.id} className={`border-b border-slate-800 p-5 last:border-0 ${notification.isRead ? 'opacity-60' : ''}`}><div className="flex gap-4"><div><h2 className="font-semibold text-white">{notification.title}</h2><p className="mt-1 text-slate-300">{notification.message}</p><p className="mt-2 text-xs text-slate-500">{new Date(notification.createdAt).toLocaleString()}</p></div>{!notification.isRead && <button type="button" onClick={() => markRead.mutate(notification.id)} className="ml-auto self-start rounded-lg border border-slate-700 px-3 py-2 text-sm text-slate-300">Mark read</button>}</div></article>)}
    </section>
  </div></main>
}
