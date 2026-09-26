export type AuthUser = {
  id: string
  email: string
  displayName: string
}

export type RegisterInput = {
  email: string
  password: string
  displayName: string
}

export type LoginInput = {
  email: string
  password: string
}