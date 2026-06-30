import {
  Box, Card, CardContent, Typography, Skeleton, Chip
} from '@mui/material'
import { useQuery } from '@tanstack/react-query'
import { motion } from 'framer-motion'
import { pageVariants } from '../../../shared/utils/motionVariants'
import api from '../../../shared/api/axiosInstance'
import PageHeader from '../../../shared/components/PageHeader'
import SeverityBadge from '../../../shared/components/SeverityBadge'
import EmptyState from '../../../shared/components/EmptyState'
import { Assignment as AuditIcon } from '@mui/icons-material'

function useAuditLogs() {
  return useQuery({
    queryKey: ['auditLogs'],
    queryFn: () => api.get('/dashboard/audit?pageSize=100').then(r => r.data),
  })
}

function formatEventType(s) {
  return s.replace(/([A-Z])/g, ' $1').trim()
}

export default function AuditPage() {
  const { data, isLoading } = useAuditLogs()

  return (
    <motion.div variants={pageVariants} initial="initial" animate="animate">
      <PageHeader title="Audit Logs" subtitle="Security event history for your organization" />

      <Card>
        <CardContent>
          {isLoading ? (
            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
              {[...Array(10)].map((_, i) => <Skeleton key={i} height={52} sx={{ borderRadius: 1 }} />)}
            </Box>
          ) : !data?.items?.length ? (
            <EmptyState icon={AuditIcon} title="No audit logs" subtitle="Security events will appear here" />
          ) : (
            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
              {data.items.map((log) => (
                <Box
                  key={log.id}
                  sx={{
                    display: 'flex',
                    alignItems: 'center',
                    gap: 2,
                    px: 2,
                    py: 1.5,
                    borderRadius: 2,
                    bgcolor: log.isSecurityCritical
                      ? 'rgba(239,68,68,0.05)'
                      : 'rgba(148,163,184,0.04)',
                    borderLeft: '3px solid',
                    borderColor: log.isSecurityCritical ? 'error.main' : 'transparent',
                    flexWrap: 'wrap',
                  }}
                >
                  <SeverityBadge severity={log.severity} />
                  <Chip
                    label={formatEventType(log.eventType)}
                    size="small"
                    sx={{ bgcolor: 'rgba(99,102,241,0.1)', color: 'primary.light', fontWeight: 600 }}
                  />
                  <Box sx={{ flex: 1, minWidth: 120 }}>
                    {log.ipAddress && (
                      <Typography variant="caption" sx={{ color: 'text.secondary', fontFamily: 'monospace' }}>
                        {log.ipAddress}
                      </Typography>
                    )}
                  </Box>
                  <Typography variant="caption" sx={{ color: 'text.disabled', whiteSpace: 'nowrap' }}>
                    {new Date(log.occurredAtUtc).toLocaleString()}
                  </Typography>
                </Box>
              ))}
            </Box>
          )}
        </CardContent>
      </Card>
    </motion.div>
  )
}
