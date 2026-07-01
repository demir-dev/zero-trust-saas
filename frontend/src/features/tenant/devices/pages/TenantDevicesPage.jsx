import { useState } from 'react'
import {
  Box, Grid, Card, CardContent, Typography, IconButton, Tooltip,
  Skeleton, Stack, Chip,
} from '@mui/material'
import {
  Block as BlockIcon,
  RemoveCircle as RevokeIcon,
  VerifiedUser as TrustIcon,
  LockOpen as UnblockIcon,
  Devices as DevicesIcon,
  LocationOn, Language, Computer,
  AccessTime as TimeIcon,
  StarBorder as CurrentIcon,
} from '@mui/icons-material'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { motion } from 'framer-motion'
import { cardContainerVariants, cardVariants } from '../../../../shared/utils/motionVariants'
import api from '../../../../shared/api/axiosInstance'
import PageHeader from '../../../../shared/components/PageHeader'
import StatusChip from '../../../../shared/components/StatusChip'
import EmptyState from '../../../../shared/components/EmptyState'
import ConfirmDialog from '../../../../shared/components/ConfirmDialog'
import { useAuth } from '../../../auth/store/authStore'

function useDevices() {
  return useQuery({
    queryKey: ['tenant', 'devices'],
    queryFn: () => api.get('/devices').then(r => r.data),
  })
}

function formatDate(val) {
  if (!val) return null
  const d = new Date(val)
  return d.toLocaleString(undefined, { dateStyle: 'medium', timeStyle: 'short' })
}

export default function TenantDevicesPage() {
  const queryClient = useQueryClient()
  const { deviceId: currentDeviceId } = useAuth()
  const { data: devices, isLoading } = useDevices()
  const [confirmBlock, setConfirmBlock] = useState(null) // device id to block
  const [confirmRevoke, setConfirmRevoke] = useState(null) // device id to revoke

  const revokeMutation = useMutation({
    mutationFn: (id) => api.post(`/devices/${id}/revoke`),
    onSuccess: () => {
      setConfirmRevoke(null)
      queryClient.invalidateQueries({ queryKey: ['tenant', 'devices'] })
    },
  })

  const blockMutation = useMutation({
    mutationFn: (id) => api.post(`/devices/${id}/block`),
    onSuccess: () => {
      setConfirmBlock(null)
      queryClient.invalidateQueries({ queryKey: ['tenant', 'devices'] })
    },
  })

  const trustMutation = useMutation({
    mutationFn: (id) => api.post(`/devices/${id}/trust`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['tenant', 'devices'] }),
  })

  const unblockMutation = useMutation({
    mutationFn: (id) => api.post(`/devices/${id}/unblock`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['tenant', 'devices'] }),
  })

  return (
    <Box>
      <PageHeader title="Devices" subtitle="Manage trusted devices for your organization" />

      {isLoading ? (
        <Grid container spacing={2}>
          {[...Array(6)].map((_, i) => (
            <Grid item xs={12} sm={6} md={4} key={i}>
              <Skeleton variant="rectangular" height={200} sx={{ borderRadius: 3 }} />
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
            {devices.map((d) => {
              const isCurrent = currentDeviceId && d.id === currentDeviceId
              return (
                <Grid item xs={12} sm={6} md={4} key={d.id}>
                  <motion.div variants={cardVariants}>
                    <Card sx={{
                      height: '100%',
                      border: '1px solid',
                      borderColor: isCurrent
                        ? 'primary.main'
                        : d.status === 'Trusted'
                          ? 'rgba(34,197,94,0.2)'
                          : d.status === 'Blocked'
                            ? 'rgba(239,68,68,0.2)'
                            : 'divider',
                      transition: 'transform 0.2s',
                      '&:hover': { transform: 'translateY(-2px)' },
                    }}>
                      <CardContent sx={{ p: 2.5 }}>
                        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 1 }}>
                          <Box sx={{ flex: 1, minWidth: 0 }}>
                            <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.75, flexWrap: 'wrap' }}>
                              <Typography variant="subtitle2" fontWeight={700} noWrap>{d.name}</Typography>
                              {isCurrent && (
                                <Chip
                                  icon={<CurrentIcon sx={{ fontSize: '14px !important' }} />}
                                  label="This device"
                                  size="small"
                                  color="primary"
                                  variant="outlined"
                                  sx={{ height: 20, fontSize: '0.65rem' }}
                                />
                              )}
                            </Box>
                          </Box>
                          <StatusChip status={d.status} />
                        </Box>

                        <Stack spacing={0.4} sx={{ mb: 1.5 }}>
                          <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                            <Computer sx={{ fontSize: 13, color: 'text.disabled' }} />
                            <Typography variant="caption" color="text.secondary">
                              {[d.browser, d.operatingSystem].filter(Boolean).join(' / ') || 'Unknown'}
                            </Typography>
                          </Box>
                          <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                            <Language sx={{ fontSize: 13, color: 'text.disabled' }} />
                            <Typography variant="caption" color="text.secondary" sx={{ fontFamily: 'monospace' }}>
                              {d.ipAddress}
                            </Typography>
                          </Box>
                          {d.country && (
                            <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                              <LocationOn sx={{ fontSize: 13, color: 'text.disabled' }} />
                              <Typography variant="caption" color="text.secondary">{d.country}</Typography>
                            </Box>
                          )}
                          {d.lastSeenAtUtc && (
                            <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                              <TimeIcon sx={{ fontSize: 13, color: 'text.disabled' }} />
                              <Typography variant="caption" color="text.secondary">
                                Last seen {formatDate(d.lastSeenAtUtc)}
                              </Typography>
                            </Box>
                          )}
                          {d.lastLoginAtUtc && (
                            <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                              <TimeIcon sx={{ fontSize: 13, color: 'text.disabled' }} />
                              <Typography variant="caption" color="text.secondary">
                                Last login {formatDate(d.lastLoginAtUtc)}
                              </Typography>
                            </Box>
                          )}
                          <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                            <TimeIcon sx={{ fontSize: 13, color: 'text.disabled' }} />
                            <Typography variant="caption" color="text.secondary">
                              First seen {formatDate(d.createdAtUtc)}
                            </Typography>
                          </Box>
                        </Stack>

                        <Box sx={{ display: 'flex', justifyContent: 'flex-end', gap: 0.5 }}>
                          {d.status === 'Pending' && (
                            <Tooltip title="Trust device">
                              <IconButton
                                size="small"
                                color="success"
                                onClick={() => trustMutation.mutate(d.id)}
                                disabled={trustMutation.isPending}
                              >
                                <TrustIcon fontSize="small" />
                              </IconButton>
                            </Tooltip>
                          )}
                          {d.status === 'Blocked' && (
                            <Tooltip title="Unblock device">
                              <IconButton
                                size="small"
                                color="info"
                                onClick={() => unblockMutation.mutate(d.id)}
                                disabled={unblockMutation.isPending}
                              >
                                <UnblockIcon fontSize="small" />
                              </IconButton>
                            </Tooltip>
                          )}
                          {d.status !== 'Revoked' && (
                            <Tooltip title="Revoke device">
                              <IconButton
                                size="small"
                                color="warning"
                                onClick={() => setConfirmRevoke(d.id)}
                              >
                                <RevokeIcon fontSize="small" />
                              </IconButton>
                            </Tooltip>
                          )}
                          {d.status !== 'Blocked' && d.status !== 'Revoked' && (
                            <Tooltip title="Block device">
                              <IconButton
                                size="small"
                                color="error"
                                onClick={() => setConfirmBlock(d.id)}
                              >
                                <BlockIcon fontSize="small" />
                              </IconButton>
                            </Tooltip>
                          )}
                        </Box>
                      </CardContent>
                    </Card>
                  </motion.div>
                </Grid>
              )
            })}
          </Grid>
        </motion.div>
      )}

      <ConfirmDialog
        open={!!confirmBlock}
        title="Block device?"
        message="The device will be blocked immediately. Any active sessions on this device will be terminated."
        onConfirm={() => blockMutation.mutate(confirmBlock)}
        onCancel={() => setConfirmBlock(null)}
        confirmColor="error"
      />
      <ConfirmDialog
        open={!!confirmRevoke}
        title="Revoke device?"
        message="The device will be revoked and removed from trusted devices. Active sessions on this device will be terminated."
        onConfirm={() => revokeMutation.mutate(confirmRevoke)}
        onCancel={() => setConfirmRevoke(null)}
        confirmColor="warning"
      />
    </Box>
  )
}
