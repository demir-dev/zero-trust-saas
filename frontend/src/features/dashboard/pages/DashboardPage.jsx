import { Grid, Box, Typography, Card, CardContent, Skeleton } from '@mui/material'
import {
  People as PeopleIcon,
  Business as BusinessIcon,
  PhonelinkLock as MfaIcon,
  Devices as DevicesIcon,
  Block as BlockIcon,
  Security as SecurityIcon,
  EventNote as AuditIcon,
  Warning as WarningIcon,
  Error as ErrorIcon,
} from '@mui/icons-material'
import { useQuery } from '@tanstack/react-query'
import { motion } from 'framer-motion'
import { cardContainerVariants } from '../../../shared/utils/motionVariants'
import api from '../../../shared/api/axiosInstance'
import StatCard from '../../../shared/components/StatCard'
import PageHeader from '../../../shared/components/PageHeader'
import SeverityBadge from '../../../shared/components/SeverityBadge'

function useSecurityOverview() {
  return useQuery({
    queryKey: ['securityOverview'],
    queryFn: () => api.get('/dashboard/security-overview').then(r => r.data),
  })
}

function useRecentAudit() {
  return useQuery({
    queryKey: ['recentAudit'],
    queryFn: () => api.get('/dashboard/audit?pageSize=10').then(r => r.data),
  })
}

function formatDate(d) {
  if (!d) return '—'
  return new Date(d).toLocaleString()
}

export default function DashboardPage() {
  const { data: overview, isLoading } = useSecurityOverview()
  const { data: auditData, isLoading: auditLoading } = useRecentAudit()

  const stats = [
    { label: 'Total Tenants', value: overview?.totalTenants, icon: BusinessIcon, color: 'primary' },
    { label: 'Total Users', value: overview?.totalUsers, icon: PeopleIcon, color: 'secondary' },
    { label: 'MFA Enabled', value: overview?.mfaEnabledCount, icon: MfaIcon, color: 'success' },
    { label: 'Trusted Devices', value: overview?.trustedDevicesCount, icon: DevicesIcon, color: 'info' },
    { label: 'Blocked Devices', value: overview?.blockedDevicesCount, icon: BlockIcon, color: 'error' },
    { label: 'Failed Logins', value: overview?.failedLoginCount, icon: ErrorIcon, color: 'warning' },
    { label: 'Suspicious Events', value: overview?.suspiciousEventCount, icon: WarningIcon, color: 'warning' },
    { label: 'Audit Events', value: overview?.auditLogCount, icon: AuditIcon, color: 'secondary' },
  ]

  return (
    <Box>
      <PageHeader
        title="Security Overview"
        subtitle="Real-time view of your Zero Trust security posture"
      />

      <motion.div variants={cardContainerVariants} initial="hidden" animate="visible">
        <Grid container spacing={2} sx={{ mb: 4 }}>
          {stats.map((s) => (
            <Grid item xs={12} sm={6} md={3} key={s.label}>
              <StatCard {...s} loading={isLoading} />
            </Grid>
          ))}
        </Grid>
      </motion.div>

      <Card>
        <CardContent>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2 }}>
            <SecurityIcon sx={{ color: 'primary.main' }} />
            <Typography variant="h6" sx={{ fontWeight: 600 }}>Recent Security Events</Typography>
          </Box>

          {auditLoading ? (
            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
              {[...Array(5)].map((_, i) => <Skeleton key={i} variant="rectangular" height={44} sx={{ borderRadius: 1 }} />)}
            </Box>
          ) : auditData?.items?.length === 0 ? (
            <Typography variant="body2" sx={{ color: 'text.secondary', textAlign: 'center', py: 4 }}>
              No audit events yet.
            </Typography>
          ) : (
            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
              {auditData?.items?.map((log) => (
                <Box
                  key={log.id}
                  sx={{
                    display: 'flex', alignItems: 'center', gap: 2,
                    px: 2, py: 1.5,
                    bgcolor: 'rgba(148,163,184,0.04)',
                    borderRadius: 2,
                    borderLeft: '3px solid',
                    borderColor: log.isSecurityCritical ? 'error.main' : 'primary.main',
                  }}
                >
                  <SeverityBadge severity={log.severity} />
                  <Typography variant="body2" sx={{ flex: 1, fontWeight: 500 }}>
                    {log.eventType.replace(/([A-Z])/g, ' $1').trim()}
                  </Typography>
                  <Typography variant="caption" sx={{ color: 'text.disabled', whiteSpace: 'nowrap' }}>
                    {formatDate(log.occurredAtUtc)}
                  </Typography>
                </Box>
              ))}
            </Box>
          )}
        </CardContent>
      </Card>
    </Box>
  )
}
