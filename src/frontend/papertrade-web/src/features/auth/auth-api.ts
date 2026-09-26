import {
  ApiError,
  apiRequest,
} from '../../lib/api-client'
import type {
  AuthUser,
  LoginInput,
  RegisterInput,
} from './auth-types'

export function register(
  input: RegisterInput,
): Promise<AuthUser> {
  return apiRequest<AuthUser>('/api/auth/register', {
    method: 'POST',
    body: JSON.stringify(input),
  })
}

export function login(
  input: LoginInput,
): Promise<AuthUser> {
  return apiRequest<AuthUser>('/api/auth/login', {
    method: 'POST',
    body: JSON.stringify(input),
  })
}

export function logout(): Promise<void> {
  return apiRequest<void>('/api/auth/logout', {
    method: 'POST',
  })
}

export async function getCurrentUser(): Promise<AuthUser | null> {
  try {
    return await apiRequest<AuthUser>('/api/auth/me')
  } catch (error) {
    if (error instanceof ApiError && error.status === 401) {
      return null
    }

    throw error
  }
}