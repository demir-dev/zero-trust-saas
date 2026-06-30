import { Outlet, useNavigate, useParams, useLocation } from 'react-router-dom'
import {
  Box, Tabs, Tab, Typography, Chip, Skeleton, Button,
} from '@mui/material'
import {
  People as UsersIcon,
  AdminPanelSettings as RolesIcon,
  Assignment as AuditIcon,
  Devices as DevicesIcon,
  ArrowBack as BackIcon,
  Business as TenantIcon,
} from '@mui/icons-material'
import { useQuery } from '@tanstack/react-query'
import api from '../../../../shared/api/axiosInstance'
import StatusChip from '../../../../shared/components/StatusChip'

const TABS = [
  { path: 'users',   label: 'Users',    icon: UsersIcon },
  { path: 'roles',   label: 'Roles',    icon: RolesIcon },
  { path: 'audit',   label: 'Audit',    icon: AuditIcon },
  { path: 'devices', label: 'Devices',  icon: DevicesIcon },
]

function useTenant(tenantId) {
  return useQuery({
    queryKey: ['platform', 'tenant', tenantId],
    queryFn: () => api.get(`/platform/tenants/${tenantId}`).then(r => r.data),
    staleTime: 30_000,
  })
}

export default function TenantInspectionLayout() {
  const { tenantId } = useParams()
  const navigate = useNavigate()
  const location = useLocation()

  const { data: tenant, isLoading } = useTenant(tenantId)

  const currentTab = TABS.findIndex((t) =>
    location.pathname.endsWith(`/${t.path}`) ||
    location.pathname.includes(`/${t.path}/`),
  )
  const activeTab = currentTab === -1 ? 0 : currentTab

  return (
    <Box>
      {/* Header */}
      <Box sx={{ mb: 2 }}>
        <Button
          startIcon={<BackIcon />}
          onClick={() => navigate('/platform/tenants')}
          variant="text"
          color="inherit"
          size="small"
          sx={{ mb: 1.5 }}
        >
          Back to Tenants
        </Button>

        <Box sx={{ display: 'flex', alignItems: 'flex-start', gap: 1.5 }}>
          <Box
            sx={{
              width: 40, height: 40, borderRadius: 2,
              bgcolor: 'rgba(99,102,241,0.12)',
              display: 'flex', alignItems: 'center', justifyContent: 'center',
              flexShrink: 0,
            }}
          >
            <TenantIcon sx={{ color: 'primary.main', fontSize: 22 }} />
          </Box>
          <Box sx={{ flex: 1, minWidth: 0 }}>
            {isLoading ? (
              <>
                <Skeleton width={200} height={28} />
                <Skeleton width={120} height={20} sx={{ mt: 0.5 }} />
              </>
            ) : (
              <>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, flexWrap: 'wrap' }}>
                  <Typography variant="h5" fontWeight={700} noWrap>
                    {tenant?.name ?? 'Tenant'}
                  </Typography>
                  {tenant?.status && <StatusChip status={tenant.status} />}
                </Box>
                <Box sx={{ display: 'flex', gap: 2, mt: 0.5, flexWrap: 'wrap' }}>
                  <Typography variant="caption" color="text.secondary">
                    Slug: <strong>{tenant?.slug}</strong>
                  </Typography>
                  <Typography variant="caption" color="text.secondary">
                    Members: <strong>{tenant?.memberCount ?? 0}</strong>
                  </Typography>
                  {tenant?.createdAtUtc && (
                    <Typography variant="caption" color="text.secondary">
                      Created: <strong>{new Date(tenant.createdAtUtc).toLocaleDateString()}</strong>
                    </Typography>
                  )}
                </Box>
              </>
            )}
          </Box>
        </Box>
      </Box>

      {/* Tabs */}
      <Box sx={{ borderBottom: 1, borderColor: 'divider', mb: 3 }}>
        <Tabs
          value={activeTab}
          onChange={(_, newVal) => navigate(`/platform/tenants/${tenantId}/${TABS[newVal].path}`)}
          variant="scrollable"
          scrollButtons="auto"
        >
          {TABS.map(({ label, icon: Icon }) => (
            <Tab
              key={label}
              label={label}
              icon={<Icon sx={{ fontSize: 18 }} />}
              iconPosition="start"
              sx={{ minHeight: 48, textTransform: 'none', fontWeight: 500 }}
            />
          ))}
        </Tabs>
      </Box>

      {/* Page content */}
      <Outlet />
    </Box>
  )
}
