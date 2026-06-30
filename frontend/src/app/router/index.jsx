import { Routes, Route, Navigate } from 'react-router-dom'
import { usePlatformStatus } from '../providers'
import ProtectedRoute from './ProtectedRoute'
import AppLayout from '../layout/AppLayout'

import LoginPage from '../../features/auth/pages/LoginPage'
import RegisterPage from '../../features/auth/pages/RegisterPage'
import SetupWizardPage from '../../features/setup/pages/SetupWizardPage'
import DashboardPage from '../../features/dashboard/pages/DashboardPage'
import TenantsPage from '../../features/tenants/pages/TenantsPage'
import UsersPage from '../../features/users/pages/UsersPage'
import DevicesPage from '../../features/devices/pages/DevicesPage'
import RolesPage from '../../features/authorization/pages/RolesPage'
import AuditPage from '../../features/audit/pages/AuditPage'
import MfaPage from '../../features/mfa/pages/MfaPage'
import SettingsPage from '../../features/settings/pages/SettingsPage'

export default function AppRouter() {
  const { isInitialized } = usePlatformStatus()

  return (
    <Routes>
      {/* Setup Wizard — only accessible when NOT initialized */}
      <Route
        path="/setup"
        element={isInitialized ? <Navigate to="/login" replace /> : <SetupWizardPage />}
      />

      {/* Login — redirects to setup if not initialized */}
      <Route
        path="/login"
        element={!isInitialized ? <Navigate to="/setup" replace /> : <LoginPage />}
      />

      {/* Register — public registration removed */}
      <Route path="/register" element={<Navigate to="/login" replace />} />

      {/* Protected app routes */}
      <Route
        path="/"
        element={
          <ProtectedRoute>
            <AppLayout />
          </ProtectedRoute>
        }
      >
        <Route index element={<Navigate to="/dashboard" replace />} />
        <Route path="dashboard" element={<DashboardPage />} />
        <Route path="tenants" element={<TenantsPage />} />
        <Route path="users" element={<UsersPage />} />
        <Route path="devices" element={<DevicesPage />} />
        <Route path="roles" element={<RolesPage />} />
        <Route path="audit" element={<AuditPage />} />
        <Route path="mfa" element={<MfaPage />} />
        <Route path="settings" element={<SettingsPage />} />
      </Route>

      {/* Catch-all */}
      <Route
        path="*"
        element={isInitialized ? <Navigate to="/dashboard" replace /> : <Navigate to="/setup" replace />}
      />
    </Routes>
  )
}
