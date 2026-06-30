import { Box, Typography, Button } from '@mui/material'
import InboxIcon from '@mui/icons-material/Inbox'

export default function EmptyState({ icon: Icon = InboxIcon, title, subtitle, action, onAction }) {
  return (
    <Box
      sx={{
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
        py: 8,
        gap: 2,
        color: 'text.secondary',
      }}
    >
      <Icon sx={{ fontSize: 56, opacity: 0.4 }} />
      <Box sx={{ textAlign: 'center' }}>
        <Typography variant="h6" sx={{ fontWeight: 600, color: 'text.secondary' }}>
          {title}
        </Typography>
        {subtitle && (
          <Typography variant="body2" sx={{ mt: 0.5, color: 'text.disabled' }}>
            {subtitle}
          </Typography>
        )}
      </Box>
      {action && (
        <Button variant="outlined" onClick={onAction} sx={{ mt: 1 }}>
          {action}
        </Button>
      )}
    </Box>
  )
}
