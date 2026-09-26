import type { UseFormRegisterReturn } from 'react-hook-form'

type FormFieldProps = {
  id: string
  label: string
  type?: 'text' | 'email' | 'password'
  autoComplete: string
  error?: string
  registration: UseFormRegisterReturn
}

export function FormField({
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