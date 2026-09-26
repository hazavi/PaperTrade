type ProblemDetails = {
  title?: string
  detail?: string
  status?: number
  errors?: Record<string, string[]>
}

export class ApiError extends Error {
  readonly status: number
  readonly errors: Record<string, string[]>

  constructor(
    status: number,
    message: string,
    errors: Record<string, string[]> = {},
  ) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.errors = errors
  }
}

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL

if (!apiBaseUrl) {
  throw new Error('VITE_API_BASE_URL is not configured.')
}

export async function apiRequest<T>(
  path: string,
  options: RequestInit = {},
): Promise<T> {
  const headers = new Headers(options.headers)

  headers.set('Accept', 'application/json')

  if (options.body && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json')
  }

  const response = await fetch(`${apiBaseUrl}${path}`, {
    ...options,
    headers,
    credentials: 'include',
  })

  if (!response.ok) {
    const problem = await readProblemDetails(response)

    throw new ApiError(
      response.status,
      problem.title ??
        problem.detail ??
        `Request failed with status ${response.status}.`,
      problem.errors,
    )
  }

  if (response.status === 204) {
    return undefined as T
  }

  return response.json() as Promise<T>
}

async function readProblemDetails(
  response: Response,
): Promise<ProblemDetails> {
  const contentType = response.headers.get('content-type')

  if (!contentType?.includes('application/json')) {
    return {}
  }

  try {
    return (await response.json()) as ProblemDetails
  } catch {
    return {}
  }
}