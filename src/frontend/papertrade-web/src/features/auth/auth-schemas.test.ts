import { describe, expect, it } from 'vitest'
import {
  loginSchema,
  registerSchema,
} from './auth-schemas'

describe('registerSchema', () => {
  it('accepts and trims valid registration input', () => {
    const result = registerSchema.parse({
      email: '  trader@example.com  ',
      password: 'a-long-passphrase',
      displayName: '  Paper Trader  ',
    })

    expect(result).toEqual({
      email: 'trader@example.com',
      password: 'a-long-passphrase',
      displayName: 'Paper Trader',
    })
  })

  it('rejects an invalid email', () => {
    const result = registerSchema.safeParse({
      email: 'not-an-email',
      password: 'a-long-passphrase',
      displayName: 'Paper Trader',
    })

    expect(result.success).toBe(false)
  })

  it('rejects a password shorter than 12 characters', () => {
    const result = registerSchema.safeParse({
      email: 'trader@example.com',
      password: 'too-short',
      displayName: 'Paper Trader',
    })

    expect(result.success).toBe(false)
  })

  it('rejects a one-character display name', () => {
    const result = registerSchema.safeParse({
      email: 'trader@example.com',
      password: 'a-long-passphrase',
      displayName: 'A',
    })

    expect(result.success).toBe(false)
  })
})

describe('loginSchema', () => {
  it('requires a valid email and a nonempty password', () => {
    const validResult = loginSchema.safeParse({
      email: 'trader@example.com',
      password: 'x',
    })

    const emptyPasswordResult = loginSchema.safeParse({
      email: 'trader@example.com',
      password: '',
    })

    expect(validResult.success).toBe(true)
    expect(emptyPasswordResult.success).toBe(false)
  })
})