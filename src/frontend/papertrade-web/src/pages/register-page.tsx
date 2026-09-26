import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { Link, useNavigate } from 'react-router'
import { ApiError } from '../lib/api-client'
import { register as registerUser } from '../features/auth/auth-api'
import {
  registerSchema,
  type RegisterInput,
} from '../features/auth/auth-schemas'
import { authKeys } from '../features/auth/auth-queries'

export function RegisterPage() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()

  const registration = useMutation({
    mutationFn: registerUser,
  })

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<RegisterInput>({
    resolver: zodResolver(registerSchema),
    defaultValues: {
      email: '',
      password: '',
      displayName: '',
    },
  })

  async function onSubmit(values: RegisterInput) {
    try {
      const user = await registration.mutateAsync(values)

      queryClient.setQueryData(authKeys.currentUser, user)
      navigate('/dashboard', { replace: true })
    } catch (error) {
      if (error instanceof ApiError) {
        const fieldNames = [
          'email',
          'password',
          'displayName',
        ] as const

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
        message: 'Unable to create your account.',
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
          Create account
        </h1>

        <p className="mt-2 text-slate-400">
          Start with $100,000 in virtual cash.
        </p>

        <form
          className="mt-8 space-y-5"
          onSubmit={handleSubmit(onSubmit)}
          noValidate
        >
          <FormField
            id="displayName"
            label="Display name"
            autoComplete="name"
            error={errors.displayName?.message}
            registration={register('displayName')}
          />

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
            autoComplete="new-password"
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
            disabled={registration.isPending}
            className="w-full rounded-lg bg-emerald-500 px-4 py-3 font-semibold text-slate-950 transition hover:bg-emerald-400 disabled:cursor-not-allowed disabled:opacity-50"
          >
            {registration.isPending
              ? 'Creating account...'
              : 'Create account'}
          </button>
        </form>

        <p className="mt-6 text-center text-sm text-slate-400">
          Already have an account?{' '}
          <Link
            to="/login"
            className="font-medium text-emerald-400 hover:text-emerald-300"
          >
            Log in
          </Link>
        </p>
      </section>
    </main>
  )
}

type FormFieldProps = {
  id: string
  label: string
  type?: 'text' | 'email' | 'password'
  autoComplete: string
  error?: string
  registration: ReturnType<
    ReturnType<typeof useForm<RegisterInput>>['register']
  >
}

function FormField({
  id,
  label,
  type = 'text',
  autoComplete,
  error,
  registration,
}: FormFieldProps) {
  const errorId = `${id}-error`

  return (
    <div>
      <label
        htmlFor={id}
        className="block text-sm font-medium text-slate-200"
      >
        {label}
      </label>

      <input
        id={id}
        type={type}
        autoComplete={autoComplete}
        aria-invalid={Boolean(error)}
        aria-describedby={error ? errorId : undefined}
        className="mt-2 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2.5 text-white outline-none transition focus:border-emerald-500 focus:ring-2 focus:ring-emerald-500/20"
        {...registration}
      />

      {error && (
        <p
          id={errorId}
          role="alert"
          className="mt-2 text-sm text-red-400"
        >
          {error}
        </p>
      )}
    </div>
  )
}