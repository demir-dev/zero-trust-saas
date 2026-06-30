import { Box, Card, CardContent, Typography, Chip, Grid, Skeleton, Divider, Avatar } from '@mui/material'
import {
  AdminPanelSettings as RoleIcon,
  Person as PersonIcon,
  Email as EmailIcon,
  Shield as ShieldIcon,
  CheckCircle as ActiveIcon,
} from '@mui/icons-material'
import { useQuery } from '@tanstack/react-query'
import { motion } from 'framer-motion'
import { cardContainerVariants, cardVariants } from '../../../../shared/utils/motionVariants'
import api from '../../../../shared/api/axiosInstance'
import PageHeader from '../../../../shared/components/PageHeader'
import StatusChip from '../../../../shared/components/StatusChip'
import { useAuth } from '../../../auth/store/authStore'

function useCurrentUser() {
  return useQuery({
    queryKey: ['users', 'me'],
    queryFn: () => api.get('/users/me').then(r => r.data),
  })
}

function usePlatformRoles() {
  return useQuery({
    queryKey: ['authorization', 'platform-roles'],
    queryFn: () => api.get('/authorization/roles').then(r => r.data),
    select: (data) => data.filter(r => r.tenantId === null || r.tenantId === undefined),
  })
}

export default function PlatformUsersPage() {
  const { platformRoles: myRoles } = useAuth()
  const { data: me, isLoading: meLoading } = useCurrentUser()
  const { data: roles, isLoading: rolesLoading } = usePlatformRoles()

  return (
    <Box>
      <PageHeader
        title="Platform Users"
        subtitle="Platform-level administrators and their roles"
      />

      <Grid container spacing={3}>
        <Grid item xs={12} md={5}>
          <Card>
            <CardContent>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2 }}>
                <PersonIcon sx={{ color: 'primary.main' }} />
                <Typography variant="h6" fontWeight={600}>Your Account</Typography>
              </Box>

              {meLoading ? (
                <>
                  <Skeleton variant="circular" width={64} height={64} sx={{ mb: 2 }} />
                  <Skeleton width="60%" />
                  <Skeleton width="80%" />
                </>
              ) : me ? (
                <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1.5 }}>
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
                    <Avatar sx={{ width: 56, height: 56, bgcolor: 'primary.main', fontSize: '1.25rem' }}>
                      {me.displayName?.charAt(0)?.toUpperCase() ?? 'P'}
                    </Avatar>
                    <Box>
                      <Typography variant="subtitle1" fontWeight={600}>{me.displayName ?? '—'}</Typography>
                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                        <EmailIcon sx={{ fontSize: 14, color: 'text.secondary' }} />
                        <Typography variant="body2" color="text.secondary">{me.email}</Typography>
                      </Box>
                    </Box>
                  </Box>

                  <Divider />

                  <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
                    {[
                      { label: 'Status', value: <StatusChip status={me.status} /> },
                      { label: 'Email Verified', value: <Chip label={me.isEmailConfirmed ? 'Yes' : 'No'} size="small" color={me.isEmailConfirmed ? 'success' : 'warning'} /> },
                      { label: 'MFA', value: <Chip label={me.isMfaEnabled ? `Enabled (${me.mfaMethod})` : 'Disabled'} size="small" color={me.isMfaEnabled ? 'success' : 'default'} /> },
                      { label: 'Last Login', value: me.lastLoginUtc ? new Date(me.lastLoginUtc).toLocaleString() : 'Never' },
                    ].map(({ label, value }) => (
                      <Box key={label} sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                        <Typography variant="body2" color="text.secondary">{label}</Typography>
                        {typeof value === 'string' ? (
                          <Typography variant="body2" fontWeight={500}>{value}</Typography>
                        ) : value}
                      </Box>
                    ))}
                  </Box>

                  <Divider />

                  <Box>
                    <Typography variant="caption" color="text.secondary" sx={{ textTransform: 'uppercase', letterSpacing: 0.5 }}>
                      Platform Roles
                    </Typography>
                    <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.75, mt: 0.75 }}>
                      {myRoles.map(role => (
                        <Chip
                          key={role}
                          icon={<ShieldIcon />}
                          label={role}
                          size="small"
                          color="primary"
                          variant="outlined"
                        />
                      ))}
                    </Box>
                  </Box>
                </Box>
              ) : null}
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} md={7}>
          <Card sx={{ height: '100%' }}>
            <CardContent>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2 }}>
                <RoleIcon sx={{ color: 'primary.main' }} />
                <Typography variant="h6" fontWeight={600}>Platform Role Definitions</Typography>
              </Box>

              {rolesLoading ? (
                <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
                  {[...Array(3)].map((_, i) => <Skeleton key={i} height={80} sx={{ borderRadius: 2 }} />)}
                </Box>
              ) : (
                <motion.div variants={cardContainerVariants} initial="hidden" animate="visible">
                  <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
                    {roles?.map(role => (
                      <motion.div key={role.id} variants={cardVariants}>
                        <Card variant="outlined" sx={{ bgcolor: 'background.default' }}>
                          <CardContent sx={{ py: 1.5, '&:last-child': { pb: 1.5 } }}>
                            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 1 }}>
                              <Typography variant="subtitle2" fontWeight={600}>{role.name}</Typography>
                              <Box sx={{ display: 'flex', gap: 0.75 }}>
                                <Chip label={role.scope} size="small" />
                                {role.isSystem && <Chip label="System" size="small" color="primary" />}
                              </Box>
                            </Box>
                            {role.permissions?.length > 0 && (
                              <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5 }}>
                                {role.permissions.map(p => (
                                  <Chip key={p} label={p} size="small" sx={{ fontSize: '0.65rem', height: 20 }} />
                                ))}
                              </Box>
                            )}
                          </CardContent>
                        </Card>
                      </motion.div>
                    ))}
                    {!roles?.length && (
                      <Typography variant="body2" color="text.secondary" textAlign="center" py={4}>
                        No platform roles found.
                      </Typography>
                    )}
                  </Box>
                </motion.div>
              )}
            </CardContent>
          </Card>
        </Grid>
      </Grid>
    </Box>
  )
}
