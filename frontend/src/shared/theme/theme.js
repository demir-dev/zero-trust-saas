import { createTheme } from '@mui/material/styles'

const theme = createTheme({
  palette: {
    mode: 'dark',
    primary: {
      main: '#6366f1',
      light: '#818cf8',
      dark: '#4f46e5',
      contrastText: '#fff',
    },
    secondary: {
      main: '#22d3ee',
      light: '#67e8f9',
      dark: '#0891b2',
    },
    background: {
      default: '#0f172a',
      paper: '#1e293b',
    },
    success: {
      main: '#22c55e',
      light: '#86efac',
      dark: '#15803d',
    },
    warning: {
      main: '#f59e0b',
      light: '#fcd34d',
      dark: '#b45309',
    },
    error: {
      main: '#ef4444',
      light: '#fca5a5',
      dark: '#b91c1c',
    },
    info: {
      main: '#3b82f6',
      light: '#93c5fd',
      dark: '#1d4ed8',
    },
    text: {
      primary: '#f1f5f9',
      secondary: '#94a3b8',
      disabled: '#475569',
    },
    divider: 'rgba(148, 163, 184, 0.12)',
  },
  typography: {
    fontFamily: '"Inter", "Segoe UI", system-ui, -apple-system, sans-serif',
    h1: { fontWeight: 700, letterSpacing: '-0.025em' },
    h2: { fontWeight: 700, letterSpacing: '-0.025em' },
    h3: { fontWeight: 600, letterSpacing: '-0.015em' },
    h4: { fontWeight: 600, letterSpacing: '-0.01em' },
    h5: { fontWeight: 600 },
    h6: { fontWeight: 600 },
    subtitle1: { fontWeight: 500, color: '#94a3b8' },
    button: { fontWeight: 600, textTransform: 'none' },
  },
  shape: {
    borderRadius: 12,
  },
  components: {
    MuiCard: {
      styleOverrides: {
        root: {
          backgroundImage: 'none',
          boxShadow: '0 1px 3px 0 rgba(0,0,0,0.3), 0 1px 2px -1px rgba(0,0,0,0.3)',
          border: '1px solid rgba(148, 163, 184, 0.08)',
        },
      },
    },
    MuiPaper: {
      styleOverrides: {
        root: {
          backgroundImage: 'none',
        },
      },
    },
    MuiButton: {
      styleOverrides: {
        root: {
          borderRadius: 8,
          fontWeight: 600,
          textTransform: 'none',
          padding: '8px 20px',
        },
        contained: {
          boxShadow: 'none',
          '&:hover': {
            boxShadow: '0 4px 12px rgba(99, 102, 241, 0.4)',
          },
        },
      },
    },
    MuiTextField: {
      defaultProps: {
        variant: 'outlined',
      },
      styleOverrides: {
        root: {
          '& .MuiOutlinedInput-root': {
            borderRadius: 8,
          },
        },
      },
    },
    MuiChip: {
      styleOverrides: {
        root: {
          fontWeight: 600,
          fontSize: '0.75rem',
        },
      },
    },
    MuiTableCell: {
      styleOverrides: {
        head: {
          fontWeight: 600,
          color: '#94a3b8',
          fontSize: '0.75rem',
          textTransform: 'uppercase',
          letterSpacing: '0.05em',
        },
      },
    },
    MuiDrawer: {
      styleOverrides: {
        paper: {
          backgroundImage: 'none',
          borderRight: '1px solid rgba(148, 163, 184, 0.08)',
        },
      },
    },
    MuiAppBar: {
      styleOverrides: {
        root: {
          backgroundImage: 'none',
          boxShadow: 'none',
          borderBottom: '1px solid rgba(148, 163, 184, 0.08)',
        },
      },
    },
    MuiListItemButton: {
      styleOverrides: {
        root: {
          borderRadius: 8,
          margin: '2px 8px',
          width: 'calc(100% - 16px)',
          '&.Mui-selected': {
            backgroundColor: 'rgba(99, 102, 241, 0.15)',
            '&:hover': {
              backgroundColor: 'rgba(99, 102, 241, 0.2)',
            },
          },
        },
      },
    },
  },
})

export default theme
