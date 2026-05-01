<script setup lang="ts">
import { useThemeStore, type ThemePreference } from '@/stores/theme'

const theme = useThemeStore()

const themeOptions: { value: ThemePreference; label: string }[] = [
  { value: 'light', label: 'Light' },
  { value: 'system', label: 'System' },
  { value: 'dark', label: 'Dark' },
]
</script>

<template>
  <div class="p-6 max-w-2xl">
    <h1 class="font-display text-3xl mb-6 text-gray-900 dark:text-gray-100">Settings</h1>

    <section class="mb-8">
      <h2 class="text-sm font-semibold uppercase tracking-wider text-gray-500 mb-3">Appearance</h2>
      <div class="inline-flex rounded-md border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 p-1">
        <button
          v-for="opt in themeOptions"
          :key="opt.value"
          type="button"
          class="px-4 py-1.5 text-sm rounded transition"
          :class="theme.preference === opt.value
            ? 'bg-primary-600 text-white'
            : 'text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700'"
          @click="theme.setPreference(opt.value)"
        >{{ opt.label }}</button>
      </div>
    </section>

    <section class="mb-8">
      <h2 class="text-sm font-semibold uppercase tracking-wider text-gray-500 mb-3">Tips</h2>
      <ul class="text-sm text-gray-700 dark:text-gray-300 space-y-2 list-disc list-inside">
        <li>Click <span class="font-medium">Edit</span> in the header to toggle drag-and-drop layout mode.</li>
        <li>In edit mode, click any tile to edit its target URL or status check.</li>
        <li>Status checks (HTTP/TCP) run every 10 seconds in the background; the dot in the corner of each tile shows online/offline.</li>
        <li>Weather widgets pull from Open-Meteo (no API key needed). Set latitude/longitude in the widget's <span class="font-mono text-xs">configJson</span>.</li>
      </ul>
    </section>

    <section>
      <h2 class="text-sm font-semibold uppercase tracking-wider text-gray-500 mb-3">About</h2>
      <p class="text-sm text-gray-600 dark:text-gray-400">
        Homeboard runs locally &mdash; no auth, no cloud. Data is stored in a SQLite file at
        <span class="font-mono text-xs">~/Library/Application Support/Homeboard/homeboard.db</span> on macOS.
      </p>
    </section>
  </div>
</template>
