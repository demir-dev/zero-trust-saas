import { Routes, Route, Navigate } from 'react-router-dom'
import { usePlatformStatus } from '../providers'
import { useAuth } from '../../features/auth/store/authStore'

import RequireAuth from './guards/RequireAuth'
import RequirePlatform from './guards/RequirePlatform'
import RequireTenant from './guards/RequireTenant'
import PlatformLayout from '../layouts/PlatformLayout'
import TenantLayout from '../layouts/TenantLayout'

import LoginPage from '../../features/auth/pages/LoginPage'
import SetupWizardPage from '../../features/setup/pages/SetupWizardPage'

import PlatformDashboardPage from '../../features/platform/dashboard/pages/PlatformDashboardPage'
import TenantsManagementPage from '../../features/platform/tenants/pages/TenantsManagementPage'
import TenantInspectionLayout from '../../features/platform/tenants/pages/TenantInspectionLayout'
import TenantInspectionUsersPage from '../../features/platform/tenants/pages/TenantInspectionUsersPage'
import TenantInspectionRolesPage from '../../features/platform/tenants/pages/TenantInspectionRolesPage'
import TenantInspectionAuditPage from '../../features/platform/tenants/pages/TenantInspectionAuditPage'
import TenantInspectionDevicesPage from '../../features/platform/tenants/pages/TenantInspectionDevicesPage'
import PlatformUsersPage from '../../features/platform/users/pages/PlatformUsersPage'
import GlobalAuditPage from '../../features/platform/audit/pages/GlobalAuditPage'
import PlatformSettingsPage from '../../features/platform/settings/pages/PlatformSettingsPage'

import TenantDashboardPage from '../../features/tenant/dashboard/pages/TenantDashboardPage'
import TenantUsersPage from '../../features/tenant/users/pages/TenantUsersPage'
import TenantRolesPage from '../../features/tenant/roles/pages/TenantRolesPage'
import TenantDevicesPage from '../../features/tenant/devices/pages/TenantDevicesPage'
import SecurityCenterPage from '../../features/tenant/security/pages/SecurityCenterPage'
import TenantAuditPage from '../../features/tenant/audit/pages/TenantAuditPage'

import ProfilePage from '../../features/profile/pages/ProfilePage'

function RootRedirect() {
  const { isAuthenticated, isPlatformUser, hasTenantContext } = useAuth()
  const { isInitialized } = usePlatformStatus()

  if (!isInitialized) return <Navigate to="/setup" replace />
  if (!isAuthenticated) return <Navigate to="/login" replace />
  if (isPlatformUser) return <Navigate to="/platform/dashboard" replace />
  if (hasTenantContext) return <Navigate to="/tenant/dashboard" replace />
  return <Navigate to="/login" replace />
}

export default function AppRouter() {
  const { isInitialized } = usePlatformStatus()

  return (
    <Routes>
      {/* Public — setup */}
      <Route
        path="/setup"
        element={isInitialized ? <Navigate to="/login" replace /> : <SetupWizardPage />}
      />

      {/* Public — login */}
      <Route
        path="/login"
        element={!isInitialized ? <Navigate to="/setup" replace /> : <LoginPage />}
      />

      {/* Platform administration */}
      <Route
        path="/platform"
        element={
          <RequireAuth>
            <RequirePlatform>
              <PlatformLayout />
            </RequirePlatform>
          </RequireAuth>
        }
      >
        <Route index element={<Navigate to="/platform/dashboard" replace />} />
        <Route path="dashboard" element={<PlatformDashboardPage />} />
        <Route path="tenants" element={<TenantsManagementPage />} />
        <Route path="tenants/:tenantId" element={<TenantInspectionLayout />}>
          <Route index element={<Navigate to="users" replace />} />
          <Route path="users" element={<TenantInspectionUsersPage />} />
          <Route path="roles" element={<TenantInspectionRolesPage />} />
          <Route path="audit" element={<TenantInspectionAuditPage />} />
          <Route path="devices" element={<TenantInspectionDevicesPage />} />
        </Route>
        <Route path="users" element={<PlatformUsersPage />} />
        <Route path="audit" element={<GlobalAuditPage />} />
        <Route path="settings" element={<PlatformSettingsPage />} />
      </Route>

      {/* Tenant administration */}
      <Route
        path="/tenant"
        element={
          <RequireAuth>
            <RequireTenant>
              <TenantLayout />
            </RequireTenant>
          </RequireAuth>
        }
      >
        <Route index element={<Navigate to="/tenant/dashboard" replace />} />
        <Route path="dashboard" element={<TenantDashboardPage />} />
        <Route path="users" element={<TenantUsersPage />} />
        <Route path="roles" element={<TenantRolesPage />} />
        <Route path="devices" element={<TenantDevicesPage />} />
        <Route path="security" element={<SecurityCenterPage />} />
        <Route path="audit" element={<TenantAuditPage />} />
      </Route>

      {/* Profile — accessible from both modes */}
      <Route
        path="/profile"
        element={
          <RequireAuth>
            <ProfilePage />
          </RequireAuth>
        }
      />

      {/* Root redirect */}
      <Route path="/" element={<RootRedirect />} />

      {/* Catch-all */}
      <Route
        path="*"
        element={isInitialized ? <Navigate to="/" replace /> : <Navigate to="/setup" replace />}
      />
    </Routes>
  )
}
