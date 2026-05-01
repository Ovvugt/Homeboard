export function readCookie(name: string): string | null {
  const match = document.cookie.match(new RegExp('(?:^|; )' + encodeURIComponent(name) + '=([^;]*)'))
  return match ? decodeURIComponent(match[1]) : null
}

export function writeCookie(name: string, value: string, maxAgeDays = 365) {
  const maxAge = maxAgeDays * 24 * 60 * 60
  document.cookie = `${encodeURIComponent(name)}=${encodeURIComponent(value)}; path=/; max-age=${maxAge}; SameSite=Lax`
}
