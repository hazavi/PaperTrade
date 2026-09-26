import { Link } from 'react-router'

export function LoginPage() {
  return (
    <main className="flex min-h-screen items-center justify-center px-6">
      <section className="w-full max-w-md rounded-2xl border border-slate-800 bg-slate-900 p-8">
        <p className="text-sm font-medium uppercase tracking-widest text-emerald-400">
          PaperTrade
        </p>
        <h1 className="mt-3 text-3xl font-bold text-white">Log in</h1>
        <p className="mt-3 text-slate-400">
          The login form is the next step.
        </p>
        <Link
          to="/register"
          className="mt-6 inline-block text-emerald-400 hover:text-emerald-300"
        >
          Create an account
        </Link>
      </section>
    </main>
  )
}