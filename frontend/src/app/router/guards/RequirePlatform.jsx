import { Navigate } from 'react-router-dom'
import { useAuth } from '../../../features/auth/store/authStore'

export default function RequirePlatform({ children }) {
  const { isAuthenticated, isPlatformUser, hasTenantContext } = useAuth()

  if (!isAuthenticated) return <Navigate to="/login" replace />
  if (!isPlatformUser) {
    return <Navigate to={hasTenantContext ? '/tenant' : '/login'} replace />
  }

  return children
}
