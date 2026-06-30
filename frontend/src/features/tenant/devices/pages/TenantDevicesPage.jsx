import {
  Box, Grid, Card, CardContent, Typography, IconButton, Tooltip,
  Skeleton, Stack,
} from '@mui/material'
import {
  Block as BlockIcon, RemoveCircle as RevokeIcon,
  Devices as DevicesIcon, LocationOn, Language, Computer,
} from '@mui/icons-material'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { motion } from 'framer-motion'
import { cardContainerVariants, cardVariants } from '../../../../shared/utils/motionVariants'
import api from '../../../../shared/api/axiosInstance'
import PageHeader from '../../../../shared/components/PageHeader'
import StatusChip from '../../../../shared/components/StatusChip'
import EmptyState from '../../../../shared/components/EmptyState'

function useDevices() {
  return useQuery({
    queryKey: ['tenant', 'devices'],
    queryFn: () => api.get('/devices').then(r => r.data),
  })
}

export default function TenantDevicesPage() {
  const queryClient = useQueryClient()
  const { data: devices, isLoading } = useDevices()

  const revokeMutation = useMutation({
    mutationFn: (id) => api.post(`/devices/${id}/revoke`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['tenant', 'devices'] }),
  })

  const blockMutation = useMutation({
    mutationFn: (id) => api.post(`/devices/${id}/block`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['tenant', 'devices'] }),
  })

  return (
    <Box>
      <PageHeader title="Devices" subtitle="Manage trusted devices for your organization" />

      {isLoading ? (
        <Grid container spacing={2}>
          {[...Array(6)].map((_, i) => (
            <Grid item xs={12} sm={6} md={4} key={i}>
              <Skeleton variant="rectangular" height={160} sx={{ borderRadius: 3 }} />
            </Grid>
          ))}
        </Grid>
      ) : !devices?.length ? (
        <Card>
          <CardContent>
            <EmptyState icon={DevicesIcon} title="No devices registered" subtitle="Trust a device to see it listed here" />
          </CardContent>
        </Card>
      ) : (
        <motion.div variants={cardContainerVariants} initial="hidden" animate="visible">
          <Grid container spacing={2}>
            {devices.map((d) => (
              <Grid item xs={12} sm={6} md={4} key={d.id}>
                <motion.div variants={cardVariants}>
                  <Card sx={{
                    height: '100%',
                    border: '1px solid',
                    borderColor: d.status === 'Trusted'
                      ? 'rgba(34,197,94,0.2)'
                      : d.status === 'Blocked'
                        ? 'rgba(239,68,68,0.2)'
                        : 'divider',
                    transition: 'transform 0.2s',
                    '&:hover': { transform: 'translateY(-2px)' },
                  }}>
                    <CardContent sx={{ p: 2.5 }}>
                      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 1.5 }}>
                        <Typography variant="subtitle2" fontWeight={700}>{d.name}</Typography>
                        <StatusChip status={d.status} />
                      </Box>

                      <Stack spacing={0.5} sx={{ mb: 2 }}>
                        <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                          <Computer sx={{ fontSize: 14, color: 'text.disabled' }} />
                          <Typography variant="caption" color="text.secondary">
                            {d.browser} / {d.operatingSystem}
                          </Typography>
                        </Box>
                        <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                          <Language sx={{ fontSize: 14, color: 'text.disabled' }} />
                          <Typography variant="caption" color="text.secondary" sx={{ fontFamily: 'monospace' }}>
                            {d.ipAddress}
                          </Typography>
                        </Box>
                        <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                          <LocationOn sx={{ fontSize: 14, color: 'text.disabled' }} />
                          <Typography variant="caption" color="text.secondary">{d.country}</Typography>
                        </Box>
                      </Stack>

                      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                        <Typography variant="caption" color="text.disabled">
                          {d.trustedAtUtc ? `Trusted ${new Date(d.trustedAtUtc).toLocaleDateString()}` : 'Not trusted'}
                        </Typography>
                        <Box>
                          {d.status !== 'Revoked' && (
                            <Tooltip title="Revoke device">
                              <IconButton size="small" color="warning" onClick={() => revokeMutation.mutate(d.id)}>
                                <RevokeIcon fontSize="small" />
                              </IconButton>
                            </Tooltip>
                          )}
                          {d.status !== 'Blocked' && d.status !== 'Revoked' && (
                            <Tooltip title="Block device">
                              <IconButton size="small" color="error" onClick={() => blockMutation.mutate(d.id)}>
                                <BlockIcon fontSize="small" />
                              </IconButton>
                            </Tooltip>
                          )}
                        </Box>
                      </Box>
                    </CardContent>
                  </Card>
                </motion.div>
              </Grid>
            ))}
          </Grid>
        </motion.div>
      )}
    </Box>
  )
}
