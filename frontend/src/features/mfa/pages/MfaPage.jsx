import { useState } from 'react'
import {
  Box, Card, CardContent, Typography, Switch, FormControlLabel,
  TextField, Button, Alert, Stack, Divider
} from '@mui/material'
import { PhonelinkLock as MfaIcon } from '@mui/icons-material'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { motion } from 'framer-motion'
import { pageVariants } from '../../../shared/utils/motionVariants'
import api from '../../../shared/api/axiosInstance'
import PageHeader from '../../../shared/components/PageHeader'
import ConfirmDialog from '../../../shared/components/ConfirmDialog'
import ProblemAlert from '../../../shared/components/ProblemAlert'

export default function MfaPage() {
  const queryClient = useQueryClient()
  const [enabled, setEnabled] = useState(false)
  const [secret, setSecret] = useState('')
  const [userId, setUserId] = useState('')
  const [confirmOpen, setConfirmOpen] = useState(false)
  const [error, setError] = useState(null)

  const enableMutation = useMutation({
    mutationFn: (data) => api.post('/auth/mfa/enable', data),
    onSuccess: () => { setEnabled(true); queryClient.invalidateQueries(['me']); setError(null) },
    onError: setError,
  })

  const disableMutation = useMutation({
    mutationFn: (id) => api.post('/auth/mfa/disable', { userId: id }),
    onSuccess: () => { setEnabled(false); queryClient.invalidateQueries(['me']); setConfirmOpen(false); setError(null) },
    onError: setError,
  })

  return (
    <motion.div variants={pageVariants} initial="initial" animate="animate">
      <PageHeader
        title="MFA Settings"
        subtitle="Configure multi-factor authentication for your account"
      />

      <Card sx={{ maxWidth: 560 }}>
        <CardContent sx={{ p: 3 }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 3 }}>
            <Box sx={{ p: 1.5, borderRadius: 2, bgcolor: 'rgba(99,102,241,0.1)' }}>
              <MfaIcon sx={{ color: 'primary.main', fontSize: 28 }} />
            </Box>
            <Box>
              <Typography variant="h6" sx={{ fontWeight: 600 }}>Multi-Factor Authentication</Typography>
              <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                Add an extra layer of security to your account
              </Typography>
            </Box>
          </Box>

          <ProblemAlert error={error} />

          {enabled ? (
            <Alert severity="success" sx={{ mb: 3, borderRadius: 2 }}>
              MFA is currently enabled for your account.
            </Alert>
          ) : (
            <Alert severity="warning" sx={{ mb: 3, borderRadius: 2 }}>
              MFA is not enabled. Enable it below for stronger security.
            </Alert>
          )}

          <Divider sx={{ mb: 3 }} />

          <Stack spacing={2}>
            <TextField
              label="User ID"
              value={userId}
              onChange={(e) => setUserId(e.target.value)}
              placeholder="Your user UUID"
              size="small"
            />

            {!enabled && (
              <>
                <TextField
                  label="TOTP Secret"
                  value={secret}
                  onChange={(e) => setSecret(e.target.value)}
                  placeholder="Base32 TOTP secret"
                  size="small"
                  helperText="Generate a TOTP secret from your authenticator app"
                />
                <Button
                  variant="contained"
                  onClick={() => enableMutation.mutate({ userId, method: 1, secret })}
                  disabled={enableMutation.isPending || !userId || !secret}
                >
                  {enableMutation.isPending ? 'Enabling…' : 'Enable TOTP MFA'}
                </Button>
              </>
            )}

            {enabled && (
              <Button
                variant="outlined"
                color="error"
                onClick={() => setConfirmOpen(true)}
              >
                Disable MFA
              </Button>
            )}
          </Stack>
        </CardContent>
      </Card>

      <ConfirmDialog
        open={confirmOpen}
        title="Disable MFA?"
        message="This will remove multi-factor authentication from your account. All active sessions will be revoked."
        onConfirm={() => disableMutation.mutate(userId)}
        onCancel={() => setConfirmOpen(false)}
      />
    </motion.div>
  )
}
