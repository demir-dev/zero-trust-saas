async function detectBrowser() {
  try {
    if (await navigator?.brave?.isBrave?.call(navigator)) return 'Brave'
  } catch { /* not Brave */ }
  const ua = navigator.userAgent
  if (ua.includes('Edg/')) return 'Edge'
  if (ua.includes('OPR/') || ua.includes('Opera')) return 'Opera'
  if (ua.includes('Chrome/')) return 'Chrome'
  if (ua.includes('Firefox/')) return 'Firefox'
  if (ua.includes('Safari/')) return 'Safari'
  return 'Unknown'
}

function detectOS() {
  const ua = navigator.userAgent
  if (/iPhone|iPad/.test(ua)) return 'iOS'
  if (/Android/.test(ua)) return 'Android'
  if (/Windows/.test(ua)) return 'Windows'
  if (/Mac OS X/.test(ua)) return 'macOS'
  if (/Linux/.test(ua)) return 'Linux'
  return 'Unknown'
}

export async function buildDeviceInfo() {
  const browser = await detectBrowser()
  const operatingSystem = detectOS()
  const tz = Intl.DateTimeFormat().resolvedOptions().timeZone
  const screen = `${window.screen.width}x${window.screen.height}`
  const deviceFingerprint = `${browser}|${operatingSystem}|${screen}|${tz}`.substring(0, 256)
  return { deviceFingerprint, browser, operatingSystem, country: 'Unknown' }
}
