import { useState } from 'react'
import { Outlet, useLocation, useNavigate } from 'react-router-dom'
import {
  Box, Drawer, AppBar, Toolbar, Typography, List, ListItem,
  ListItemButton, ListItemIcon, ListItemText, IconButton,
  Avatar, Menu, MenuItem, Divider, useMediaQuery, useTheme, Chip
} from '@mui/material'
import {
  Dashboard as DashboardIcon,
  Business as TenantsIcon,
  People as UsersIcon,
  Devices as DevicesIcon,
  AdminPanelSettings as RolesIcon,
  Assignment as AuditIcon,
  Settings as SettingsIcon,
  SecurityUpdateGood as MfaIcon,
  Menu as MenuIcon,
  Shield as ShieldIcon,
  Logout as LogoutIcon,
  AccountCircle as AccountIcon,
} from '@mui/icons-material'
import { AnimatePresence, motion } from 'framer-motion'
import { pageVariants } from '../../shared/utils/motionVariants'
import { useAuth } from '../../features/auth/store/authStore'

const DRAWER_WIDTH = 248

const navItems = [
  { path: '/dashboard', label: 'Dashboard', icon: DashboardIcon },
  { path: '/tenants', label: 'Tenants', icon: TenantsIcon },
  { path: '/users', label: 'Users', icon: UsersIcon },
  { path: '/devices', label: 'Devices', icon: DevicesIcon },
  { path: '/roles', label: 'Roles & Permissions', icon: RolesIcon },
  { path: '/audit', label: 'Audit Logs', icon: AuditIcon },
  { divider: true },
  { path: '/mfa', label: 'MFA Settings', icon: MfaIcon },
  { path: '/settings', label: 'Settings', icon: SettingsIcon },
]

export default function AppLayout() {
  const theme = useTheme()
  const isMobile = useMediaQuery(theme.breakpoints.down('md'))
  const [mobileOpen, setMobileOpen] = useState(false)
  const [anchorEl, setAnchorEl] = useState(null)
  const location = useLocation()
  const navigate = useNavigate()
  const { logout } = useAuth()

  const handleDrawerToggle = () => setMobileOpen((p) => !p)
  const handleAvatarClick = (e) => setAnchorEl(e.currentTarget)
  const handleMenuClose = () => setAnchorEl(null)

  const handleLogout = async () => {
    handleMenuClose()
    await logout()
    navigate('/login')
  }

  const drawerContent = (
    <Box sx={{ height: '100%', display: 'flex', flexDirection: 'column', bgcolor: 'background.paper' }}>
      <Box sx={{ px: 2.5, py: 2.5, display: 'flex', alignItems: 'center', gap: 1.5 }}>
        <Box
          sx={{
            width: 36, height: 36, borderRadius: 2,
            bgcolor: 'primary.main',
            display: 'flex', alignItems: 'center', justifyContent: 'center',
          }}
        >
          <ShieldIcon sx={{ color: '#fff', fontSize: 20 }} />
        </Box>
        <Box>
          <Typography variant="h6" sx={{ fontSize: '1rem', fontWeight: 700, lineHeight: 1.2 }}>
            ZeroTrust
          </Typography>
          <Typography variant="caption" sx={{ color: 'primary.main', fontWeight: 600 }}>
            SaaS Portal
          </Typography>
        </Box>
      </Box>

      <Divider sx={{ mx: 2, borderColor: 'divider' }} />

      <List sx={{ flex: 1, pt: 1.5, px: 0 }}>
        {navItems.map((item, i) => {
          if (item.divider) return <Divider key={i} sx={{ mx: 2, my: 1, borderColor: 'divider' }} />
          const selected = location.pathname === item.path || location.pathname.startsWith(item.path + '/')
          return (
            <ListItem key={item.path} disablePadding>
              <ListItemButton
                selected={selected}
                onClick={() => { navigate(item.path); if (isMobile) setMobileOpen(false) }}
                sx={{ mx: 1, px: 1.5, py: 1, minHeight: 40 }}
              >
                <ListItemIcon sx={{ minWidth: 34, color: selected ? 'primary.main' : 'text.secondary' }}>
                  <item.icon sx={{ fontSize: 20 }} />
                </ListItemIcon>
                <ListItemText
                  primary={item.label}
                  primaryTypographyProps={{
                    fontSize: '0.875rem',
                    fontWeight: selected ? 600 : 500,
                    color: selected ? 'primary.main' : 'text.primary',
                  }}
                />
              </ListItemButton>
            </ListItem>
          )
        })}
      </List>

      <Box sx={{ p: 2 }}>
        <Chip
          label="Zero Trust Architecture"
          size="small"
          sx={{ bgcolor: 'rgba(99,102,241,0.1)', color: 'primary.light', width: '100%', fontSize: '0.7rem' }}
        />
      </Box>
    </Box>
  )

  return (
    <Box sx={{ display: 'flex', minHeight: '100vh', bgcolor: 'background.default' }}>
      <AppBar
        position="fixed"
        sx={{
          width: { md: `calc(100% - ${DRAWER_WIDTH}px)` },
          ml: { md: `${DRAWER_WIDTH}px` },
          bgcolor: 'background.default',
          zIndex: (t) => t.zIndex.drawer + 1,
        }}
      >
        <Toolbar>
          <IconButton
            color="inherit"
            edge="start"
            onClick={handleDrawerToggle}
            sx={{ mr: 2, display: { md: 'none' } }}
          >
            <MenuIcon />
          </IconButton>
          <Box sx={{ flex: 1 }} />
          <IconButton onClick={handleAvatarClick} size="small">
            <Avatar sx={{ width: 34, height: 34, bgcolor: 'primary.main', fontSize: '0.9rem' }}>
              A
            </Avatar>
          </IconButton>
          <Menu anchorEl={anchorEl} open={Boolean(anchorEl)} onClose={handleMenuClose}
            PaperProps={{ sx: { mt: 1, borderRadius: 2, minWidth: 160 } }}
          >
            <MenuItem onClick={() => { navigate('/settings'); handleMenuClose() }}>
              <AccountIcon fontSize="small" sx={{ mr: 1.5 }} /> Profile
            </MenuItem>
            <Divider />
            <MenuItem onClick={handleLogout} sx={{ color: 'error.main' }}>
              <LogoutIcon fontSize="small" sx={{ mr: 1.5 }} /> Logout
            </MenuItem>
          </Menu>
        </Toolbar>
      </AppBar>

      <Box component="nav" sx={{ width: { md: DRAWER_WIDTH }, flexShrink: { md: 0 } }}>
        <Drawer
          variant="temporary"
          open={mobileOpen}
          onClose={handleDrawerToggle}
          ModalProps={{ keepMounted: true }}
          sx={{ display: { xs: 'block', md: 'none' }, '& .MuiDrawer-paper': { width: DRAWER_WIDTH } }}
        >
          {drawerContent}
        </Drawer>
        <Drawer
          variant="permanent"
          sx={{ display: { xs: 'none', md: 'block' }, '& .MuiDrawer-paper': { width: DRAWER_WIDTH, boxSizing: 'border-box' } }}
          open
        >
          {drawerContent}
        </Drawer>
      </Box>

      <Box
        component="main"
        sx={{
          flexGrow: 1,
          width: { md: `calc(100% - ${DRAWER_WIDTH}px)` },
          minHeight: '100vh',
          display: 'flex',
          flexDirection: 'column',
        }}
      >
        <Toolbar />
        <Box sx={{ flex: 1, p: { xs: 2, sm: 3 } }}>
          <AnimatePresence mode="wait">
            <motion.div
              key={location.pathname}
              variants={pageVariants}
              initial="initial"
              animate="animate"
              exit="exit"
              style={{ height: '100%' }}
            >
              <Outlet />
            </motion.div>
          </AnimatePresence>
        </Box>
      </Box>
    </Box>
  )
}
