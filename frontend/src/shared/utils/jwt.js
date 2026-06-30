export function parseJwtClaims(token) {
  try {
    const base64 = token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/')
    const payload = JSON.parse(atob(base64))
    return {
      sub: payload.sub ?? null,
      email: payload.email ?? null,
      platformRoles: [].concat(payload.platform_role ?? []),
      tenantId: payload.tenant_id ?? null,
      tenantRole: payload.tenant_role ?? null,
      permissions: [].concat(payload.permission ?? []),
      exp: payload.exp ?? null,
    }
  } catch {
    return {
      sub: null, email: null, platformRoles: [], tenantId: null,
      tenantRole: null, permissions: [], exp: null,
    }
  }
}

export function isTokenExpired(token) {
  const { exp } = parseJwtClaims(token)
  if (!exp) return true
  return Date.now() >= exp * 1000
}
