import type { ReactNode } from 'react'

type FullPageStatusProps = {
  title: string
  message: string
  action?: ReactNode
}

export function FullPageStatus({
  title,
  message,
  action,
}: FullPageStatusProps) {
  return (
    <main className="flex min-h-screen items-center justify-center px-6">
      <section className="w-full max-w-md rounded-2xl border border-slate-800 bg-slate-900 p-8 text-center shadow-xl">
        <h1 className="text-2xl font-bold text-white">{title}</h1>
        <p className="mt-3 text-slate-400">{message}</p>
        {action && <div className="mt-6">{action}</div>}
      </section>
    </main>
  )
}