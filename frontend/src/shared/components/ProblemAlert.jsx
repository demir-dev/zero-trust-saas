import { Alert, AlertTitle } from '@mui/material'

export default function ProblemAlert({ error }) {
  if (!error) return null

  const data = error.response?.data
  const title = data?.title || 'Error'
  const detail = data?.detail || error.message || 'An unexpected error occurred.'

  const severity = error.response?.status === 401
    ? 'warning'
    : error.response?.status >= 500
    ? 'error'
    : 'error'

  return (
    <Alert severity={severity} sx={{ mb: 2, borderRadius: 2 }}>
      <AlertTitle>{title}</AlertTitle>
      {detail}
    </Alert>
  )
}
