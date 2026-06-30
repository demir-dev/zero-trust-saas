import { Routes, Route, Navigate } from 'react-router-dom'
import ProtectedRoute from './ProtectedRoute'
import AppLayout from '../layout/AppLayout'

import LoginPage from '../../features/auth/pages/LoginPage'
import RegisterPage from '../../features/auth/pages/RegisterPage'
import DashboardPage from '../../features/dashboard/pages/DashboardPage'
import TenantsPage from '../../features/tenants/pages/TenantsPage'
import UsersPage from '../../features/users/pages/UsersPage'
import DevicesPage from '../../features/devices/pages/DevicesPage'
import RolesPage from '../../features/authorization/pages/RolesPage'
import AuditPage from '../../features/audit/pages/AuditPage'
import MfaPage from '../../features/mfa/pages/MfaPage'
import SettingsPage from '../../features/settings/pages/SettingsPage'

export default function AppRouter() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />

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

      <Route path="*" element={<Navigate to="/dashboard" replace />} />
    </Routes>
  )
}
