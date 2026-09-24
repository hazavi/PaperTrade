import { useEffect, useState } from 'react'

type ApiStatus = 'checking' | 'ok' | 'unavailable'

const statusText: Record<ApiStatus, string> = {
  checking: 'Checking connection...',
  ok: 'Connected',
  unavailable: 'Unavailable',
}

const statusColor: Record<ApiStatus, string> = {
  checking: 'text-amber-400',
  ok: 'text-emerald-400',
  unavailable: 'text-red-400',
}

function App() {
  const [apiStatus, setApiStatus] = useState<ApiStatus>('checking')

  useEffect(() => {
    const controller = new AbortController()
    const apiBaseUrl = import.meta.env.VITE_API_BASE_URL

    async function checkApiStatus() {
      if (!apiBaseUrl) {
        setApiStatus('unavailable')
        return
      }

      try {
        const response = await fetch(`${apiBaseUrl}/api/status`, {
          signal: controller.signal,
        })

        if (!response.ok) {
          throw new Error(`API returned ${response.status}`)
        }

        const data: unknown = await response.json()

        if (
          typeof data !== 'object' ||
          data === null ||
          !('status' in data) ||
          data.status !== 'ok'
        ) {
          throw new Error('API returned an unexpected response')
        }

        setApiStatus('ok')
      } catch (error) {
        if (error instanceof DOMException && error.name === 'AbortError') {
          return
        }

        setApiStatus('unavailable')
      }
    }

    void checkApiStatus()

    return () => {
      controller.abort()
    }
  }, [])

  return (
    <main className="flex min-h-screen items-center justify-center px-6">
      <section className="w-full max-w-xl rounded-2xl border border-slate-800 bg-slate-900 p-8 shadow-xl">
        <p className="text-sm font-medium uppercase tracking-widest text-emerald-400">
          Development environment
        </p>

        <h1 className="mt-3 text-4xl font-bold text-white">PaperTrade</h1>

        <p className="mt-4 text-slate-300">
          Practice investing with virtual money and real market data.
        </p>

        <div className="mt-8 rounded-lg bg-slate-950 p-4">
          <p className="text-sm text-slate-400">API status</p>
          <p className={`mt-1 font-semibold ${statusColor[apiStatus]}`}>
            {statusText[apiStatus]}
          </p>
        </div>
      </section>
    </main>
  )
}

export default App