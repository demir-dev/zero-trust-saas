import { Box, CircularProgress, Typography } from '@mui/material'
import { motion } from 'framer-motion'

export default function SplashScreen() {
  return (
    <Box
      sx={{
        minHeight: '100vh',
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
        bgcolor: 'background.default',
        gap: 3,
      }}
    >
      <motion.div
        initial={{ opacity: 0 }}
        animate={{ opacity: 1 }}
        transition={{ duration: 0.4 }}
      >
        <CircularProgress size={48} color="primary" thickness={3} />
      </motion.div>
      <Typography variant="body2" color="text.secondary">
        Loading…
      </Typography>
    </Box>
  )
}
