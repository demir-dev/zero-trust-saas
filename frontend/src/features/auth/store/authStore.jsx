import { createContext, useContext, useState, useCallback, useMemo, useEffect } from 'react'
import api from '../../../shared/api/axiosInstance'
import { parseJwtClaims } from '../../../shared/utils/jwt'
import { buildDeviceInfo } from '../../../shared/utils/deviceInfo'

const AuthContext = createContext(null)

let _applyTokenExternal = null
export function _registerExternalApplyToken(fn) { _applyTokenExternal = fn }
export function applyTokenExternal(token, refresh) {
  if (_applyTokenExternal) _applyTokenExternal(token, refresh)
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

  useEffect(() => {
    _registerExternalApplyToken(applyToken)
    return () => _registerExternalApplyToken(null)
  }, [applyToken])

  // Platform login — tenantSlug is null
  const login = useCallback(async (email, password, extraDeviceInfo, trustDevice = false) => {
    const deviceInfo = { ...await buildDeviceInfo(), ...extraDeviceInfo }
    const res = await api.post('/auth/login', {
      tenantSlug: null,
      email,
      password,
      trustDevice,
      ...deviceInfo,
    })
    const { accessToken: token, refreshToken, result, requiresMfa, userId, isPlatformUser } = res.data
    if (token) applyToken(token, refreshToken)
    return { result, requiresMfa, userId, accessToken: token, isPlatformUser: isPlatformUser ?? false }
  }, [applyToken])

  // Tenant login — tenantSlug provided
  const loginWithTenant = useCallback(async (email, password, tenantSlug, extraDeviceInfo, trustDevice = false) => {
    const deviceInfo = { ...await buildDeviceInfo(), ...extraDeviceInfo }
    const res = await api.post('/auth/login', {
      tenantSlug,
      email,
      password,
      trustDevice,
      ...deviceInfo,
    })
    const { accessToken: token, refreshToken, result, requiresMfa, userId } = res.data
    if (token) applyToken(token, refreshToken)
    return { result, requiresMfa, userId, accessToken: token }
  }, [applyToken])

  const verifyMfa = useCallback(async (userId, tenantSlug, code, isRecoveryCode, extraDeviceInfo, trustDevice = false) => {
    const deviceInfo = { ...await buildDeviceInfo(), ...extraDeviceInfo }
    const res = await api.post('/auth/mfa/verify', {
      userId,
      tenantSlug: tenantSlug ?? null,
      code,
      isRecoveryCode,
      trustDevice,
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
    const userId = claims?.sub ?? null
    const deviceId = claims?.deviceId ?? null
    const sessionId = claims?.sessionId ?? null

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
      userId,
      deviceId,
      sessionId,
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
