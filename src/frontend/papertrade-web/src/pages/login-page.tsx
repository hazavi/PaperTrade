import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import {
  Link,
  useLocation,
  useNavigate,
} from 'react-router'
import { FormField } from '../components/form-field'
import { login } from '../features/auth/auth-api'
import {
  loginSchema,
  type LoginInput,
} from '../features/auth/auth-schemas'
import { authKeys } from '../features/auth/auth-queries'
import { ApiError } from '../lib/api-client'

type LoginLocationState = {
  from?: string
}

export function LoginPage() {
  const navigate = useNavigate()
  const location = useLocation()
  const queryClient = useQueryClient()

  const loginMutation = useMutation({
    mutationFn: login,
  })

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<LoginInput>({
    resolver: zodResolver(loginSchema),
    defaultValues: {
      email: '',
      password: '',
    },
  })

  async function onSubmit(values: LoginInput) {
    try {
      const user = await loginMutation.mutateAsync(values)

      queryClient.setQueryData(authKeys.currentUser, user)

      const state = location.state as LoginLocationState | null
      const destination = state?.from ?? '/dashboard'

      navigate(destination, { replace: true })
    } catch (error) {
      if (error instanceof ApiError) {
        const fieldNames = ['email', 'password'] as const
        let hasFieldError = false

        for (const fieldName of fieldNames) {
          const message = error.errors[fieldName]?.[0]

          if (message) {
            setError(fieldName, {
              type: 'server',
              message,
            })
            hasFieldError = true
          }
        }

        if (!hasFieldError) {
          setError('root', {
            type: 'server',
            message: error.message,
          })
        }

        return
      }

      setError('root', {
        type: 'server',
        message: 'Unable to log in.',
      })
    }
  }

  return (
    <main className="flex min-h-screen items-center justify-center px-6 py-10">
      <section className="w-full max-w-md rounded-2xl border border-slate-800 bg-slate-900 p-8 shadow-xl">
        <p className="text-sm font-medium uppercase tracking-widest text-emerald-400">
          PaperTrade
        </p>

        <h1 className="mt-3 text-3xl font-bold text-white">
          Log in
        </h1>

        <p className="mt-2 text-slate-400">
          Continue to your virtual portfolio.
        </p>

        <form
          className="mt-8 space-y-5"
          onSubmit={handleSubmit(onSubmit)}
          noValidate
        >
          <FormField
            id="email"
            label="Email"
            type="email"
            autoComplete="email"
            error={errors.email?.message}
            registration={register('email')}
          />

          <FormField
            id="password"
            label="Password"
            type="password"
            autoComplete="current-password"
            error={errors.password?.message}
            registration={register('password')}
          />

          {errors.root?.message && (
            <p
              role="alert"
              className="rounded-lg border border-red-900 bg-red-950/50 p-3 text-sm text-red-300"
            >
              {errors.root.message}
            </p>
          )}

          <button
            type="submit"
            disabled={loginMutation.isPending}
            className="w-full rounded-lg bg-emerald-500 px-4 py-3 font-semibold text-slate-950 transition hover:bg-emerald-400 disabled:cursor-not-allowed disabled:opacity-50"
          >
            {loginMutation.isPending
              ? 'Logging in...'
              : 'Log in'}
          </button>
        </form>

        <p className="mt-6 text-center text-sm text-slate-400">
          Need an account?{' '}
          <Link
            to="/register"
            className="font-medium text-emerald-400 hover:text-emerald-300"
          >
            Register
          </Link>
        </p>
      </section>
    </main>
  )
}