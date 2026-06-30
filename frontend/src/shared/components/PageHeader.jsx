import { Box, Typography } from '@mui/material'

export default function PageHeader({ title, subtitle, action }) {
  return (
    <Box sx={{ display: 'flex', alignItems: 'flex-start', justifyContent: 'space-between', mb: 3 }}>
      <Box>
        <Typography variant="h5" sx={{ fontWeight: 700, color: 'text.primary' }}>
          {title}
        </Typography>
        {subtitle && (
          <Typography variant="body2" sx={{ color: 'text.secondary', mt: 0.5 }}>
            {subtitle}
          </Typography>
        )}
      </Box>
      {action && <Box>{action}</Box>}
    </Box>
  )
}
