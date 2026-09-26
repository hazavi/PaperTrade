import { z } from 'zod'

export const registerSchema = z.object({
  email: z
    .string()
    .trim()
    .min(1, 'Email is required.')
    .max(320, 'Email must be 320 characters or fewer.')
    .email('Enter a valid email address.'),
  password: z
    .string()
    .min(12, 'Password must be at least 12 characters.')
    .max(128, 'Password must be 128 characters or fewer.'),
  displayName: z
    .string()
    .trim()
    .min(2, 'Display name must be at least 2 characters.')
    .max(100, 'Display name must be 100 characters or fewer.'),
})

export const loginSchema = z.object({
  email: z
    .string()
    .trim()
    .min(1, 'Email is required.')
    .max(320, 'Email must be 320 characters or fewer.')
    .email('Enter a valid email address.'),
  password: z
    .string()
    .min(1, 'Password is required.')
    .max(128, 'Password must be 128 characters or fewer.'),
})

export type RegisterInput = z.infer<typeof registerSchema>
export type LoginInput = z.infer<typeof loginSchema>