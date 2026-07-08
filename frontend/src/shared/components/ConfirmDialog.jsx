import { Dialog, DialogTitle, DialogContent, DialogContentText, DialogActions, Button } from '@mui/material'

export default function ConfirmDialog({ open, title, message, onConfirm, onCancel, confirmColor = 'error', loading = false }) {
  return (
    <Dialog open={open} onClose={loading ? undefined : onCancel} PaperProps={{ sx: { borderRadius: 3 } }}>
      <DialogTitle>{title}</DialogTitle>
      <DialogContent>
        <DialogContentText>{message}</DialogContentText>
      </DialogContent>
      <DialogActions sx={{ p: 2, gap: 1 }}>
        <Button onClick={onCancel} variant="outlined" disabled={loading}>Cancel</Button>
        <Button onClick={onConfirm} variant="contained" color={confirmColor} disabled={loading}>
          {loading ? 'Working…' : 'Confirm'}
        </Button>
      </DialogActions>
    </Dialog>
  )
}
