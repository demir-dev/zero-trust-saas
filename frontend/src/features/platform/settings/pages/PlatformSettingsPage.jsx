import { Box, Card, CardContent, Typography, Chip, Button, Divider } from '@mui/material'
import {
  Shield as ShieldIcon,
  CheckCircle as CheckIcon,
  Settings as SettingsIcon,
  Person as PersonIcon,
} from '@mui/icons-material'
import { useNavigate } from 'react-router-dom'
import PageHeader from '../../../../shared/components/PageHeader'
import { useAuth } from '../../../auth/store/authStore'

export default function PlatformSettingsPage() {
  const navigate = useNavigate()
  const { platformRoles } = useAuth()

  return (
    <Box>
      <PageHeader
        title="Platform Settings"
        subtitle="Global configuration and platform information"
      />

      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 3, maxWidth: 680 }}>
        <Card>
          <CardContent>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2 }}>
              <ShieldIcon sx={{ color: 'primary.main' }} />
              <Typography variant="h6" fontWeight={600}>Platform Status</Typography>
            </Box>
            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1.5 }}>
              <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <Typography variant="body2" color="text.secondary">Initialization Status</Typography>
                <Chip icon={<CheckIcon />} label="Initialized" color="success" size="small" />
              </Box>
              <Divider />
              <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <Typography variant="body2" color="text.secondary">Architecture</Typography>
                <Typography variant="body2" fontWeight={600}>Zero Trust IAM</Typography>
              </Box>
              <Divider />
              <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <Typography variant="body2" color="text.secondary">Deployment Mode</Typography>
                <Typography variant="body2" fontWeight={600}>Single-Instance Multi-Tenant</Typography>
              </Box>
            </Box>
          </CardContent>
        </Card>

        <Card>
          <CardContent>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2 }}>
              <SettingsIcon sx={{ color: 'primary.main' }} />
              <Typography variant="h6" fontWeight={600}>Your Platform Access</Typography>
            </Box>
            <Typography variant="body2" color="text.secondary" mb={1.5}>
              You currently hold the following platform roles:
            </Typography>
            <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
              {platformRoles.map(role => (
                <Chip
                  key={role}
                  icon={<ShieldIcon />}
                  label={role}
                  color="primary"
                  variant="outlined"
                />
              ))}
            </Box>
          </CardContent>
        </Card>

        <Card>
          <CardContent>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2 }}>
              <PersonIcon sx={{ color: 'primary.main' }} />
              <Typography variant="h6" fontWeight={600}>Account</Typography>
            </Box>
            <Typography variant="body2" color="text.secondary" mb={2}>
              Manage your personal account settings, MFA configuration, and trusted devices.
            </Typography>
            <Button variant="outlined" onClick={() => navigate('/profile')}>
              Open Profile Settings
            </Button>
          </CardContent>
        </Card>
      </Box>
    </Box>
  )
}
