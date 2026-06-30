import { createContext, useContext, useState, useCallback } from 'react'
import api from '../../../shared/api/axiosInstance'

const AuthContext = createContext(null)

export function AuthProvider({ children }) {
  const [accessToken, setAccessToken] = useState(() => sessionStorage.getItem('accessToken'))
  const [user, setUser] = useState(null)

  const login = useCallback(async (credentials) => {
    const res = await api.post('/auth/login', credentials)
    const { accessToken: token, refreshToken } = res.data
    if (token) {
      sessionStorage.setItem('accessToken', token)
      setAccessToken(token)
    }
    if (refreshToken) {
      localStorage.setItem('refreshToken', refreshToken)
    }
    return res.data
  }, [])

  const register = useCallback(async (data) => {
    const res = await api.post('/auth/register', data)
    return res.data
  }, [])

  const logout = useCallback(async () => {
    try {
      await api.post('/auth/logout')
    } catch {}
    sessionStorage.removeItem('accessToken')
    localStorage.removeItem('refreshToken')
    setAccessToken(null)
    setUser(null)
  }, [])

  const isAuthenticated = !!accessToken

  return (
    <AuthContext.Provider value={{ accessToken, user, setUser, login, register, logout, isAuthenticated }}>
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used inside AuthProvider')
  return ctx
}
