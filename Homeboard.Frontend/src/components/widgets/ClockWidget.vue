<script setup lang="ts">
import { computed } from 'vue'
import type { WidgetDto } from '@/types/board'
import { useNow } from '@/composables/useNow'

const props = defineProps<{ widget: WidgetDto }>()

interface ClockConfig {
  format?: '12h' | '24h'
  timezone?: string
  label?: string
}

const config = computed<ClockConfig>(() => {
  try { return JSON.parse(props.widget.configJson) as ClockConfig } catch { return {} }
})

const now = useNow()

const time = computed(() => {
  const opts: Intl.DateTimeFormatOptions = {
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
    hour12: config.value.format === '12h',
    timeZone: config.value.timezone,
  }
  return new Intl.DateTimeFormat(undefined, opts).format(now.value)
})

const date = computed(() => {
  const opts: Intl.DateTimeFormatOptions = {
    weekday: 'short',
    month: 'short',
    day: 'numeric',
    timeZone: config.value.timezone,
  }
  return new Intl.DateTimeFormat(undefined, opts).format(now.value)
})
</script>

<template>
  <div class="h-full w-full rounded-xl bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 shadow-sm flex flex-col items-center justify-center p-3 select-none">
    <div class="font-display text-3xl md:text-4xl text-gray-900 dark:text-gray-100 tabular-nums">{{ time }}</div>
    <div class="text-xs text-gray-500 mt-1">{{ config.label || date }}</div>
  </div>
</template>
