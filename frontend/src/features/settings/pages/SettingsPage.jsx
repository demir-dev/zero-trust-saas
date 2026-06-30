import { Box, Card, CardContent, Typography, Skeleton, Divider, Stack, Chip } from '@mui/material'
import {
  AccountCircle as AccountIcon,
  Business as BusinessIcon,
  Security as SecurityIcon,
  VerifiedUser as VerifiedIcon,
} from '@mui/icons-material'
import { useQuery } from '@tanstack/react-query'
import { motion } from 'framer-motion'
import { pageVariants } from '../../../shared/utils/motionVariants'
import api from '../../../shared/api/axiosInstance'
import PageHeader from '../../../shared/components/PageHeader'
import StatusChip from '../../../shared/components/StatusChip'

function useMe() {
  return useQuery({
    queryKey: ['me'],
    queryFn: () => api.get('/users/me').then(r => r.data),
    retry: false,
  })
}

function InfoRow({ label, value, mono }) {
  return (
    <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', py: 1.5 }}>
      <Typography variant="body2" sx={{ color: 'text.secondary', fontWeight: 500 }}>{label}</Typography>
      <Typography variant="body2" sx={{ fontWeight: 600, fontFamily: mono ? 'monospace' : undefined, color: 'text.primary' }}>
        {value || '—'}
      </Typography>
    </Box>
  )
}

export default function SettingsPage() {
  const { data: me, isLoading } = useMe()

  return (
    <motion.div variants={pageVariants} initial="initial" animate="animate">
      <PageHeader title="Profile & Settings" subtitle="Your account information" />

      <Box sx={{ maxWidth: 560 }}>
        <Card sx={{ mb: 2 }}>
          <CardContent sx={{ p: 3 }}>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, mb: 2 }}>
              <AccountIcon sx={{ color: 'primary.main' }} />
              <Typography variant="h6" sx={{ fontWeight: 600 }}>Account</Typography>
            </Box>

            {isLoading ? (
              <Stack spacing={1}>{[...Array(4)].map((_, i) => <Skeleton key={i} height={36} />)}</Stack>
            ) : !me ? (
              <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                Could not load profile. Please ensure you are authenticated.
              </Typography>
            ) : (
              <>
                <InfoRow label="Email" value={me.email} />
                <Divider sx={{ borderColor: 'divider' }} />
                <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', py: 1.5 }}>
                  <Typography variant="body2" sx={{ color: 'text.secondary', fontWeight: 500 }}>Status</Typography>
                  <StatusChip status={me.status} />
                </Box>
                <Divider sx={{ borderColor: 'divider' }} />
                <InfoRow label="User ID" value={me.id} mono />
                <Divider sx={{ borderColor: 'divider' }} />
                <InfoRow label="Tenant ID" value={me.tenantId} mono />
                <Divider sx={{ borderColor: 'divider' }} />
                <InfoRow label="Registered" value={me.registeredAtUtc ? new Date(me.registeredAtUtc).toLocaleString() : '—'} />
                <Divider sx={{ borderColor: 'divider' }} />
                <InfoRow label="Last Login" value={me.lastLoginUtc ? new Date(me.lastLoginUtc).toLocaleString() : '—'} />
              </>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardContent sx={{ p: 3 }}>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, mb: 2 }}>
              <SecurityIcon sx={{ color: 'primary.main' }} />
              <Typography variant="h6" sx={{ fontWeight: 600 }}>Security</Typography>
            </Box>

            {isLoading ? (
              <Stack spacing={1}>{[...Array(2)].map((_, i) => <Skeleton key={i} height={36} />)}</Stack>
            ) : me && (
              <>
                <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', py: 1.5 }}>
                  <Typography variant="body2" sx={{ color: 'text.secondary', fontWeight: 500 }}>Email Verified</Typography>
                  <Chip
                    icon={me.isEmailConfirmed ? <VerifiedIcon sx={{ fontSize: 14 }} /> : undefined}
                    label={me.isEmailConfirmed ? 'Verified' : 'Not verified'}
                    color={me.isEmailConfirmed ? 'success' : 'warning'}
                    size="small"
                  />
                </Box>
                <Divider sx={{ borderColor: 'divider' }} />
                <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', py: 1.5 }}>
                  <Typography variant="body2" sx={{ color: 'text.secondary', fontWeight: 500 }}>MFA</Typography>
                  <Chip
                    label={me.isMfaEnabled ? `Enabled (${me.mfaMethod})` : 'Disabled'}
                    color={me.isMfaEnabled ? 'success' : 'default'}
                    size="small"
                  />
                </Box>
              </>
            )}
          </CardContent>
        </Card>
      </Box>
    </motion.div>
  )
}
