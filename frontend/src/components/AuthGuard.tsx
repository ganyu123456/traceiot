import { Navigate } from 'react-router-dom'
import { useAuthStore } from '@/store/authStore'
import { ReactNode } from 'react'

export default function AuthGuard({ children }: { children: ReactNode }) {
  const isLoggedIn = useAuthStore(s => s.isLoggedIn)
  if (!isLoggedIn) return <Navigate to="/login" replace />
  return <>{children}</>
}
