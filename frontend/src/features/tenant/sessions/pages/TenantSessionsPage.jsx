import { useState } from 'react'
import {
  Box, Grid, Card, CardContent, Typography, IconButton, Tooltip,
  Skeleton, Stack, Chip,
} from '@mui/material'
import {
  RemoveCircle as RevokeIcon,
  Computer as ComputerIcon,
  Language as LanguageIcon,
  LocationOn as LocationIcon,
  AccessTime as TimeIcon,
  StarBorder as CurrentIcon,
  MeetingRoom as SessionIcon,
} from '@mui/icons-material'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { motion } from 'framer-motion'
import { cardContainerVariants, cardVariants } from '../../../../shared/utils/motionVariants'
import api from '../../../../shared/api/axiosInstance'
import PageHeader from '../../../../shared/components/PageHeader'
import EmptyState from '../../../../shared/components/EmptyState'
import ConfirmDialog from '../../../../shared/components/ConfirmDialog'
import { useAuth } from '../../../auth/store/authStore'

function formatDate(val) {
  if (!val) return null
  return new Date(val).toLocaleString(undefined, { dateStyle: 'medium', timeStyle: 'short' })
}

function SessionStatusChip({ status, isExpired }) {
  if (isExpired) return <Chip label="Expired" size="small" color="default" />
  if (status === 1) return <Chip label="Active" size="small" color="success" />
  if (status === 3) return <Chip label="Revoked" size="small" color="error" />
  return <Chip label={String(status)} size="small" />
}

export default function TenantSessionsPage() {
  const queryClient = useQueryClient()
  const { userId, sessionId: currentSessionId, tenantId } = useAuth()
  const [confirmRevoke, setConfirmRevoke] = useState(null)

  const { data: sessions, isLoading } = useQuery({
    queryKey: ['tenant', 'user-sessions', userId],
    queryFn: () => api.get(`/users/${userId}/sessions`).then(r => r.data),
    enabled: !!userId,
  })

  const revokeMutation = useMutation({
    mutationFn: (sid) => api.post(`/users/${userId}/sessions/${sid}/revoke`),
    onSuccess: () => {
      setConfirmRevoke(null)
      queryClient.invalidateQueries({ queryKey: ['tenant', 'user-sessions', userId] })
    },
    onError: () => setConfirmRevoke(null),
  })

  return (
    <Box>
      <PageHeader
        title="Sessions"
        subtitle="Review and revoke login sessions across your devices. Shows active sessions and recent history from the past 7 days."
      />

      {isLoading ? (
        <Grid container spacing={2}>
          {[...Array(4)].map((_, i) => (
            <Grid item xs={12} sm={6} md={4} key={i}>
              <Skeleton variant="rectangular" height={220} sx={{ borderRadius: 3 }} />
            </Grid>
          ))}
        </Grid>
      ) : !sessions?.length ? (
        <Card>
          <CardContent>
            <EmptyState
              icon={SessionIcon}
              title="No sessions"
              subtitle="No login sessions found in the past 7 days"
            />
          </CardContent>
        </Card>
      ) : (
        <motion.div variants={cardContainerVariants} initial="hidden" animate="visible">
          <Grid container spacing={2}>
            {sessions.map((s) => {
              const isCurrent = currentSessionId && s.id === currentSessionId
              const isExpired = new Date(s.expiresAtUtc) <= new Date()
              return (
                <Grid item xs={12} sm={6} md={4} key={s.id}>
                  <motion.div variants={cardVariants}>
                    <Card sx={{
                      height: '100%',
                      border: '1px solid',
                      borderColor: isCurrent
                        ? 'primary.main'
                        : s.status === 3
                          ? 'rgba(239,68,68,0.2)'
                          : 'divider',
                      transition: 'transform 0.2s',
                      '&:hover': { transform: 'translateY(-2px)' },
                    }}>
                      <CardContent sx={{ p: 2.5 }}>
                        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 1 }}>
                          <Box sx={{ flex: 1, minWidth: 0, display: 'flex', alignItems: 'center', gap: 0.75, flexWrap: 'wrap' }}>
                            {isCurrent && (
                              <Chip
                                icon={<CurrentIcon sx={{ fontSize: '14px !important' }} />}
                                label="Current session"
                                size="small"
                                color="primary"
                                variant="outlined"
                                sx={{ height: 20, fontSize: '0.65rem' }}
                              />
                            )}
                          </Box>
                          <SessionStatusChip status={s.status} isExpired={isExpired} />
                        </Box>

                        <Stack spacing={0.4} sx={{ mb: 1.5 }}>
                          {(s.browser || s.operatingSystem) && (
                            <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                              <ComputerIcon sx={{ fontSize: 13, color: 'text.disabled' }} />
                              <Typography variant="caption" color="text.secondary">
                                {[s.browser, s.operatingSystem].filter(Boolean).join(' on ')}
                              </Typography>
                            </Box>
                          )}
                          {s.ipAddress && (
                            <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                              <LanguageIcon sx={{ fontSize: 13, color: 'text.disabled' }} />
                              <Typography variant="caption" color="text.secondary" sx={{ fontFamily: 'monospace' }}>
                                {s.ipAddress}
                              </Typography>
                            </Box>
                          )}
                          {s.country && (
                            <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                              <LocationIcon sx={{ fontSize: 13, color: 'text.disabled' }} />
                              <Typography variant="caption" color="text.secondary">{s.country}</Typography>
                            </Box>
                          )}
                          <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                            <TimeIcon sx={{ fontSize: 13, color: 'text.disabled' }} />
                            <Typography variant="caption" color="text.secondary">
                              Started {formatDate(s.createdAtUtc)}
                            </Typography>
                          </Box>
                          {s.lastSeenAtUtc && (
                            <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                              <TimeIcon sx={{ fontSize: 13, color: 'text.disabled' }} />
                              <Typography variant="caption" color="text.secondary">
                                Last seen {formatDate(s.lastSeenAtUtc)}
                              </Typography>
                            </Box>
                          )}
                          {s.status === 1 && (
                            <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                              <TimeIcon sx={{ fontSize: 13, color: 'text.disabled' }} />
                              <Typography variant="caption" color={isExpired ? 'error.main' : 'text.secondary'}>
                                Expires {formatDate(s.expiresAtUtc)}
                              </Typography>
                            </Box>
                          )}
                          {s.revokedAtUtc && (
                            <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                              <TimeIcon sx={{ fontSize: 13, color: 'text.disabled' }} />
                              <Typography variant="caption" color="error.main">
                                Revoked {formatDate(s.revokedAtUtc)}
                              </Typography>
                            </Box>
                          )}
                        </Stack>

                        <Box sx={{ display: 'flex', justifyContent: 'flex-end' }}>
                          {s.status === 1 && !isCurrent && (
                            <Tooltip title="Revoke session">
                              <IconButton
                                size="small"
                                color="warning"
                                onClick={() => setConfirmRevoke(s.id)}
                              >
                                <RevokeIcon fontSize="small" />
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
        open={!!confirmRevoke}
        title="Revoke session?"
        message="This will immediately terminate the selected session. The device will be logged out."
        onConfirm={() => revokeMutation.mutate(confirmRevoke)}
        onCancel={() => setConfirmRevoke(null)}
        confirmColor="warning"
        loading={revokeMutation.isPending}
      />
    </Box>
  )
}
