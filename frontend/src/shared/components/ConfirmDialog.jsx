import { Dialog, DialogTitle, DialogContent, DialogContentText, DialogActions, Button } from '@mui/material'

export default function ConfirmDialog({ open, title, message, onConfirm, onCancel, confirmColor = 'error' }) {
  return (
    <Dialog open={open} onClose={onCancel} PaperProps={{ sx: { borderRadius: 3 } }}>
      <DialogTitle>{title}</DialogTitle>
      <DialogContent>
        <DialogContentText>{message}</DialogContentText>
      </DialogContent>
      <DialogActions sx={{ p: 2, gap: 1 }}>
        <Button onClick={onCancel} variant="outlined">Cancel</Button>
        <Button onClick={onConfirm} variant="contained" color={confirmColor}>
          Confirm
        </Button>
      </DialogActions>
    </Dialog>
  )
}
