import { Navigate } from 'react-router-dom'
import { useAuth } from '../../../features/auth/store/authStore'

export default function RequireTenant({ children }) {
  const { isAuthenticated, hasTenantContext, isPlatformUser } = useAuth()

  if (!isAuthenticated) return <Navigate to="/login" replace />
  if (!hasTenantContext) {
    return <Navigate to={isPlatformUser ? '/platform' : '/login'} replace />
  }

  return children
}
