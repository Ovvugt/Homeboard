import { defineStore } from 'pinia'
import { computed, ref, watch } from 'vue'
import { usePreferredDark } from '@vueuse/core'
import { readCookie, writeCookie } from '@/utils/cookie'

export type ThemePreference = 'light' | 'dark' | 'system'

const COOKIE_KEY = 'homeboard.theme'
const DEFAULT: ThemePreference = 'light'

function loadInitial(): ThemePreference {
  const raw = readCookie(COOKIE_KEY)
  return raw === 'light' || raw === 'dark' || raw === 'system' ? raw : DEFAULT
}

export const useThemeStore = defineStore('theme', () => {
  const preference = ref<ThemePreference>(loadInitial())
  const prefersDark = usePreferredDark()

  const isDark = computed(() => {
    if (preference.value === 'system') return prefersDark.value
    return preference.value === 'dark'
  })

  function setPreference(value: ThemePreference) {
    preference.value = value
  }

  function toggle() {
    if (preference.value === 'light') preference.value = 'dark'
    else if (preference.value === 'dark') preference.value = 'system'
    else preference.value = 'light'
  }

  function applyTheme() {
    if (isDark.value) document.documentElement.classList.add('dark')
    else document.documentElement.classList.remove('dark')
  }

  watch(preference, v => writeCookie(COOKIE_KEY, v))
  watch(isDark, applyTheme, { immediate: true })

  return { preference, isDark, setPreference, toggle }
})
