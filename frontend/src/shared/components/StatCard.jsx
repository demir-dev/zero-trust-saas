import { Card, CardContent, Box, Typography, Skeleton } from '@mui/material'
import { motion } from 'framer-motion'
import { cardVariants } from '../utils/motionVariants'

export default function StatCard({ icon: Icon, label, value, color = 'primary', loading }) {
  return (
    <motion.div variants={cardVariants}>
      <Card
        sx={{
          height: '100%',
          background: `linear-gradient(135deg, rgba(${colorToRgb(color)}, 0.08) 0%, rgba(${colorToRgb(color)}, 0.02) 100%)`,
          border: `1px solid rgba(${colorToRgb(color)}, 0.15)`,
          transition: 'transform 0.2s, box-shadow 0.2s',
          '&:hover': {
            transform: 'translateY(-2px)',
            boxShadow: `0 8px 24px rgba(${colorToRgb(color)}, 0.2)`,
          },
        }}
      >
        <CardContent sx={{ p: 3 }}>
          <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 2 }}>
            <Box
              sx={{
                width: 44,
                height: 44,
                borderRadius: 2,
                bgcolor: `rgba(${colorToRgb(color)}, 0.12)`,
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
              }}
            >
              <Icon sx={{ color: `${color}.main`, fontSize: 22 }} />
            </Box>
          </Box>
          {loading ? (
            <>
              <Skeleton variant="text" width={60} height={40} />
              <Skeleton variant="text" width={100} />
            </>
          ) : (
            <>
              <Typography variant="h4" sx={{ fontWeight: 700, lineHeight: 1 }}>
                {value ?? '—'}
              </Typography>
              <Typography variant="body2" sx={{ color: 'text.secondary', mt: 0.5, fontWeight: 500 }}>
                {label}
              </Typography>
            </>
          )}
        </CardContent>
      </Card>
    </motion.div>
  )
}

function colorToRgb(color) {
  const map = {
    primary: '99, 102, 241',
    secondary: '34, 211, 238',
    success: '34, 197, 94',
    warning: '245, 158, 11',
    error: '239, 68, 68',
    info: '59, 130, 246',
  }
  return map[color] || map.primary
}
