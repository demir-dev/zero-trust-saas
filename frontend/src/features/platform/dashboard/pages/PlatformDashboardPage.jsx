import { Box, Grid, Card, CardContent, Typography, Skeleton, Divider } from '@mui/material'
import {
  Business as TenantsIcon,
  CheckCircle as ActiveIcon,
  PauseCircle as SuspendedIcon,
  AdminPanelSettings as UsersIcon,
  Security as SecurityIcon,
  Warning as WarningIcon,
} from '@mui/icons-material'
import { useQuery } from '@tanstack/react-query'
import { motion } from 'framer-motion'
import { cardContainerVariants } from '../../../../shared/utils/motionVariants'
import api from '../../../../shared/api/axiosInstance'
import StatCard from '../../../../shared/components/StatCard'
import PageHeader from '../../../../shared/components/PageHeader'
import SeverityBadge from '../../../../shared/components/SeverityBadge'

function useOverview() {
  return useQuery({
    queryKey: ['platform', 'overview'],
    queryFn: () => api.get('/dashboard/security-overview').then(r => r.data),
  })
}

function useRecentAudit() {
  return useQuery({
    queryKey: ['platform', 'recentAudit'],
    queryFn: () => api.get('/dashboard/audit?pageSize=8').then(r => r.data),
  })
}

function fmt(d) {
  if (!d) return '—'
  return new Date(d).toLocaleString(undefined, { dateStyle: 'short', timeStyle: 'short' })
}

export default function PlatformDashboardPage() {
  const { data: overview, isLoading } = useOverview()
  const { data: auditData, isLoading: auditLoading } = useRecentAudit()

  const stats = [
    { label: 'Total Tenants', value: overview?.totalTenants, icon: TenantsIcon, color: 'primary' },
    { label: 'Total Users', value: overview?.totalUsers, icon: ActiveIcon, color: 'success' },
    { label: 'Suspicious Events', value: overview?.suspiciousEventCount, icon: WarningIcon, color: 'warning' },
    { label: 'Failed Logins', value: overview?.failedLoginCount, icon: SecurityIcon, color: 'error' },
  ]

  return (
    <Box>
      <PageHeader
        title="Platform Dashboard"
        subtitle="Global view of your Zero Trust IAM platform"
      />

      <motion.div variants={cardContainerVariants} initial="hidden" animate="visible">
        <Grid container spacing={2} sx={{ mb: 3 }}>
          {stats.map((s) => (
            <Grid item xs={12} sm={6} md={3} key={s.label}>
              <StatCard {...s} loading={isLoading} />
            </Grid>
          ))}
        </Grid>
      </motion.div>

      <Grid container spacing={3}>
        <Grid item xs={12} md={8}>
          <Card>
            <CardContent>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2 }}>
                <SecurityIcon sx={{ color: 'primary.main' }} />
                <Typography variant="h6" fontWeight={600}>Recent Security Events</Typography>
              </Box>
              {auditLoading ? (
                <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
                  {[...Array(6)].map((_, i) => (
                    <Skeleton key={i} variant="rectangular" height={44} sx={{ borderRadius: 1 }} />
                  ))}
                </Box>
              ) : !auditData?.items?.length ? (
                <Typography variant="body2" color="text.secondary" textAlign="center" py={4}>
                  No audit events yet.
                </Typography>
              ) : (
                <Box sx={{ display: 'flex', flexDirection: 'column', gap: 0.75 }}>
                  {auditData.items.map((log) => (
                    <Box
                      key={log.id}
                      sx={{
                        display: 'flex', alignItems: 'center', gap: 2,
                        px: 2, py: 1.25,
                        bgcolor: 'rgba(148,163,184,0.04)',
                        borderRadius: 1.5,
                        borderLeft: '3px solid',
                        borderColor: log.isSecurityCritical ? 'error.main' : 'primary.main',
                      }}
                    >
                      <SeverityBadge severity={log.severity} />
                      <Typography variant="body2" fontWeight={500} sx={{ flex: 1 }}>
                        {log.eventType.replace(/([A-Z])/g, ' $1').trim()}
                      </Typography>
                      {log.ipAddress && (
                        <Typography variant="caption" color="text.disabled" sx={{ fontFamily: 'monospace' }}>
                          {log.ipAddress}
                        </Typography>
                      )}
                      <Typography variant="caption" color="text.disabled" whiteSpace="nowrap">
                        {fmt(log.occurredAtUtc)}
                      </Typography>
                    </Box>
                  ))}
                </Box>
              )}
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} md={4}>
          <Card sx={{ height: '100%' }}>
            <CardContent>
              <Typography variant="h6" fontWeight={600} mb={2}>Platform Health</Typography>
              <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
                {[
                  { label: 'MFA Enabled Users', value: overview?.mfaEnabledCount, icon: '🔐' },
                  { label: 'Trusted Devices', value: overview?.trustedDevicesCount, icon: '✅' },
                  { label: 'Revoked Devices', value: overview?.revokedDevicesCount, icon: '🚫' },
                  { label: 'Blocked Devices', value: overview?.blockedDevicesCount, icon: '⛔' },
                  { label: 'Total Audit Events', value: overview?.auditLogCount, icon: '📋' },
                ].map(({ label, value, icon }) => (
                  <Box key={label}>
                    <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                      <Typography variant="body2" color="text.secondary">
                        {icon} {label}
                      </Typography>
                      {isLoading ? (
                        <Skeleton width={32} />
                      ) : (
                        <Typography variant="body2" fontWeight={600}>{value ?? '—'}</Typography>
                      )}
                    </Box>
                    <Divider sx={{ mt: 1 }} />
                  </Box>
                ))}
              </Box>
            </CardContent>
          </Card>
        </Grid>
      </Grid>
    </Box>
  )
}
