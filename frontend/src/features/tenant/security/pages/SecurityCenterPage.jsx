import { Box, Grid, Card, CardContent, Typography, Chip, Skeleton, Divider } from '@mui/material'
import {
  Warning as WarningIcon,
  Error as ErrorIcon,
  LockPerson as LockIcon,
  Security as SecurityIcon,
  Block as BlockIcon,
} from '@mui/icons-material'
import { useQuery } from '@tanstack/react-query'
import { motion } from 'framer-motion'
import { cardContainerVariants } from '../../../../shared/utils/motionVariants'
import api from '../../../../shared/api/axiosInstance'
import StatCard from '../../../../shared/components/StatCard'
import PageHeader from '../../../../shared/components/PageHeader'
import SeverityBadge from '../../../../shared/components/SeverityBadge'

function useSecurityOverview() {
  return useQuery({
    queryKey: ['tenant', 'security', 'overview'],
    queryFn: () => api.get('/dashboard/security-overview').then(r => r.data),
  })
}

function useAuditLogs() {
  return useQuery({
    queryKey: ['tenant', 'security', 'audit'],
    queryFn: () => api.get('/dashboard/audit?pageSize=20').then(r => r.data),
  })
}

function useLockedUsers() {
  return useQuery({
    queryKey: ['tenant', 'security', 'locked-users'],
    queryFn: () => api.get('/users?pageSize=100').then(r => r.data),
    select: (d) => (d.items ?? []).filter(u => u.status === 'Locked'),
  })
}

export default function SecurityCenterPage() {
  const { data: overview, isLoading: overviewLoading } = useSecurityOverview()
  const { data: auditData, isLoading: auditLoading } = useAuditLogs()
  const { data: lockedUsers, isLoading: usersLoading } = useLockedUsers()

  const suspiciousLogs = auditData?.items?.filter(l => l.isSecurityCritical || l.severity === 'Critical') ?? []
  const failedLogins = auditData?.items?.filter(l => l.eventType?.toLowerCase().includes('loginfailed')) ?? []

  const stats = [
    { label: 'Suspicious Events', value: overview?.suspiciousEventCount, icon: WarningIcon, color: 'warning' },
    { label: 'Failed Logins', value: overview?.failedLoginCount, icon: ErrorIcon, color: 'error' },
    { label: 'Locked Users', value: lockedUsers?.length, icon: LockIcon, color: 'warning' },
    { label: 'Blocked Devices', value: overview?.blockedDevicesCount, icon: BlockIcon, color: 'error' },
  ]

  return (
    <Box>
      <PageHeader
        title="Security Center"
        subtitle="Threat overview for your organization"
      />

      <motion.div variants={cardContainerVariants} initial="hidden" animate="visible">
        <Grid container spacing={2} sx={{ mb: 4 }}>
          {stats.map((s) => (
            <Grid item xs={12} sm={6} md={3} key={s.label}>
              <StatCard {...s} loading={overviewLoading || usersLoading} />
            </Grid>
          ))}
        </Grid>
      </motion.div>

      <Grid container spacing={3}>
        <Grid item xs={12} md={6}>
          <Card sx={{ height: '100%' }}>
            <CardContent>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2 }}>
                <SecurityIcon sx={{ color: 'error.main' }} />
                <Typography variant="h6" fontWeight={600}>Critical Security Events</Typography>
              </Box>
              {auditLoading ? (
                <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
                  {[...Array(4)].map((_, i) => <Skeleton key={i} height={52} sx={{ borderRadius: 1 }} />)}
                </Box>
              ) : suspiciousLogs.length === 0 ? (
                <Typography variant="body2" color="text.secondary" textAlign="center" py={4}>
                  No critical events detected.
                </Typography>
              ) : (
                <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
                  {suspiciousLogs.slice(0, 8).map(log => (
                    <Box
                      key={log.id}
                      sx={{
                        display: 'flex', alignItems: 'center', gap: 2,
                        px: 2, py: 1.5,
                        bgcolor: 'rgba(239,68,68,0.04)',
                        borderRadius: 2,
                        borderLeft: '3px solid',
                        borderColor: 'error.main',
                      }}
                    >
                      <SeverityBadge severity={log.severity} />
                      <Box sx={{ flex: 1, minWidth: 0 }}>
                        <Typography variant="body2" fontWeight={500} noWrap>
                          {log.eventType?.replace(/([A-Z])/g, ' $1').trim()}
                        </Typography>
                        <Typography variant="caption" color="text.disabled">
                          {log.ipAddress ?? '—'} · {log.occurredAtUtc ? new Date(log.occurredAtUtc).toLocaleString() : '—'}
                        </Typography>
                      </Box>
                    </Box>
                  ))}
                </Box>
              )}
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} md={6}>
          <Card sx={{ height: '100%' }}>
            <CardContent>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2 }}>
                <LockIcon sx={{ color: 'warning.main' }} />
                <Typography variant="h6" fontWeight={600}>Locked Users</Typography>
              </Box>
              {usersLoading ? (
                <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
                  {[...Array(3)].map((_, i) => <Skeleton key={i} height={52} sx={{ borderRadius: 1 }} />)}
                </Box>
              ) : !lockedUsers?.length ? (
                <Typography variant="body2" color="text.secondary" textAlign="center" py={4}>
                  No locked users. All accounts are accessible.
                </Typography>
              ) : (
                <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
                  {lockedUsers.map(user => (
                    <Box
                      key={user.id}
                      sx={{
                        display: 'flex', alignItems: 'center', justifyContent: 'space-between',
                        px: 2, py: 1.5,
                        bgcolor: 'rgba(234,179,8,0.04)',
                        borderRadius: 2,
                        borderLeft: '3px solid',
                        borderColor: 'warning.main',
                      }}
                    >
                      <Box>
                        <Typography variant="body2" fontWeight={500}>{user.displayName ?? user.email}</Typography>
                        <Typography variant="caption" color="text.disabled">{user.email}</Typography>
                      </Box>
                      <Chip label="Locked" size="small" color="warning" />
                    </Box>
                  ))}
                </Box>
              )}
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12}>
          <Card>
            <CardContent>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2 }}>
                <ErrorIcon sx={{ color: 'error.main' }} />
                <Typography variant="h6" fontWeight={600}>Recent Failed Login Attempts</Typography>
              </Box>
              <Divider sx={{ mb: 2 }} />
              {auditLoading ? (
                <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
                  {[...Array(3)].map((_, i) => <Skeleton key={i} height={40} sx={{ borderRadius: 1 }} />)}
                </Box>
              ) : failedLogins.length === 0 ? (
                <Typography variant="body2" color="text.secondary" textAlign="center" py={3}>
                  No failed logins recorded.
                </Typography>
              ) : (
                <Box sx={{ display: 'flex', flexDirection: 'column', gap: 0.75 }}>
                  {failedLogins.slice(0, 6).map(log => (
                    <Box
                      key={log.id}
                      sx={{ display: 'flex', alignItems: 'center', gap: 2, px: 1.5, py: 1, borderRadius: 1 }}
                    >
                      <SeverityBadge severity={log.severity} />
                      <Typography variant="body2" sx={{ flex: 1 }}>
                        {log.eventType?.replace(/([A-Z])/g, ' $1').trim()}
                      </Typography>
                      <Typography variant="caption" color="text.disabled" sx={{ fontFamily: 'monospace' }}>
                        {log.ipAddress ?? '—'}
                      </Typography>
                      <Typography variant="caption" color="text.disabled" whiteSpace="nowrap">
                        {log.occurredAtUtc ? new Date(log.occurredAtUtc).toLocaleString(undefined, { dateStyle: 'short', timeStyle: 'short' }) : '—'}
                      </Typography>
                    </Box>
                  ))}
                </Box>
              )}
            </CardContent>
          </Card>
        </Grid>
      </Grid>
    </Box>
  )
}
