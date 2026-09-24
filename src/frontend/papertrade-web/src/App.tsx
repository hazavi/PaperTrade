function App() {
  return (
    <main className="flex min-h-screen items-center justify-center px-6">
      <section className="w-full max-w-xl rounded-2xl border border-slate-800 bg-slate-900 p-8 shadow-xl">
        <p className="text-sm font-medium uppercase tracking-widest text-emerald-400">
          Development environment
        </p>

        <h1 className="mt-3 text-4xl font-bold text-white">
          PaperTrade
        </h1>

        <p className="mt-4 text-slate-300">
          Practice investing with virtual money and real market data.
        </p>

        <div className="mt-8 rounded-lg bg-slate-950 p-4">
          <p className="text-sm text-slate-400">API status</p>
          <p className="mt-1 font-semibold text-amber-400">
            Not connected yet
          </p>
        </div>
      </section>
    </main>
  )
}

export default App