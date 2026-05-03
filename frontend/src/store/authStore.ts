import { create } from 'zustand'

interface UserInfo {
  userName: string
  email?: string
  roles: string[]
  token: string
  expiresAt: string
}

interface AuthState {
  user: UserInfo | null
  isLoggedIn: boolean
  login: (user: UserInfo) => void
  logout: () => void
}

const storedUser = localStorage.getItem('user')

export const useAuthStore = create<AuthState>(set => ({
  user: storedUser ? JSON.parse(storedUser) : null,
  isLoggedIn: !!storedUser,

  login: user => {
    localStorage.setItem('token', user.token)
    localStorage.setItem('user', JSON.stringify(user))
    set({ user, isLoggedIn: true })
  },

  logout: () => {
    localStorage.removeItem('token')
    localStorage.removeItem('user')
    set({ user: null, isLoggedIn: false })
  },
}))
