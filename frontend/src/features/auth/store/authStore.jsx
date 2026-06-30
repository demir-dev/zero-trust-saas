import { createContext, useContext, useState, useCallback, useMemo } from 'react'
import api from '../../../shared/api/axiosInstance'
import { parseJwtClaims } from '../../../shared/utils/jwt'

const AuthContext = createContext(null)

function buildDeviceInfo() {
  return {
    deviceFingerprint: navigator.userAgent.substring(0, 50),
    country: 'Unknown',
    browser: navigator.userAgent.split(' ').slice(-1)[0] || 'Unknown',
    operatingSystem: navigator.platform || 'Unknown',
  }
}

function storeTokens(accessToken, refreshToken) {
  if (accessToken) sessionStorage.setItem('accessToken', accessToken)
  if (refreshToken) localStorage.setItem('refreshToken', refreshToken)
}

function clearTokens() {
  sessionStorage.removeItem('accessToken')
  localStorage.removeItem('refreshToken')
}

function claimsFromStoredToken() {
  const token = sessionStorage.getItem('accessToken')
  if (!token) return null
  return parseJwtClaims(token)
}

export function AuthProvider({ children }) {
  const [accessToken, setAccessToken] = useState(() => sessionStorage.getItem('accessToken'))
  const [claims, setClaims] = useState(() => claimsFromStoredToken())
  const [user, setUser] = useState(null)

  const applyToken = useCallback((token, refresh) => {
    storeTokens(token, refresh)
    setAccessToken(token)
    setClaims(token ? parseJwtClaims(token) : null)
  }, [])

  // Platform login — tenantSlug is null
  const login = useCallback(async (email, password, extraDeviceInfo) => {
    const deviceInfo = { ...buildDeviceInfo(), ...extraDeviceInfo }
    const res = await api.post('/auth/login', {
      tenantSlug: null,
      email,
      password,
      ...deviceInfo,
    })
    const { accessToken: token, refreshToken, result, requiresMfa, userId } = res.data
    if (token) applyToken(token, refreshToken)
    return { result, requiresMfa, userId, accessToken: token }
  }, [applyToken])

  // Tenant login — tenantSlug provided
  const loginWithTenant = useCallback(async (email, password, tenantSlug, extraDeviceInfo) => {
    const deviceInfo = { ...buildDeviceInfo(), ...extraDeviceInfo }
    const res = await api.post('/auth/login', {
      tenantSlug,
      email,
      password,
      ...deviceInfo,
    })
    const { accessToken: token, refreshToken, result, requiresMfa, userId } = res.data
    if (token) applyToken(token, refreshToken)
    return { result, requiresMfa, userId, accessToken: token }
  }, [applyToken])

  const verifyMfa = useCallback(async (userId, tenantSlug, code, isRecoveryCode, extraDeviceInfo) => {
    const deviceInfo = { ...buildDeviceInfo(), ...extraDeviceInfo }
    const res = await api.post('/auth/mfa/verify', {
      userId,
      tenantSlug: tenantSlug ?? null,
      code,
      isRecoveryCode,
      ...deviceInfo,
    })
    const { accessToken: token, refreshToken, result, requiresMfa, userId: uid } = res.data
    if (token) applyToken(token, refreshToken)
    return { result, requiresMfa, userId: uid, accessToken: token }
  }, [applyToken])

  const logout = useCallback(async () => {
    try { await api.post('/auth/logout') } catch { /* ignore */ }
    clearTokens()
    setAccessToken(null)
    setClaims(null)
    setUser(null)
  }, [])

  const value = useMemo(() => {
    const isAuthenticated = !!accessToken
    const platformRoles = claims?.platformRoles ?? []
    const tenantId = claims?.tenantId ?? null
    const tenantRole = claims?.tenantRole ?? null
    const permissions = claims?.permissions ?? []
    const isPlatformUser = platformRoles.length > 0
    const hasTenantContext = !!tenantId

    const hasPermission = (code) => isPlatformUser || permissions.includes(code)

    return {
      accessToken,
      user, setUser,
      platformRoles,
      tenantId,
      tenantRole,
      permissions,
      isAuthenticated,
      isPlatformUser,
      hasTenantContext,
      hasPermission,
      login,
      loginWithTenant,
      verifyMfa,
      logout,
      applyToken,
    }
  }, [accessToken, claims, user, setUser, login, loginWithTenant, verifyMfa, logout, applyToken])

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used inside AuthProvider')
  return ctx
}
