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

function offsetMinutes(timeZone: string | undefined, instant: Date): number {
  const parts = new Intl.DateTimeFormat('en-US', {
    timeZone,
    hour12: false,
    year: 'numeric', month: '2-digit', day: '2-digit',
    hour: '2-digit', minute: '2-digit', second: '2-digit',
  }).formatToParts(instant)
  const get = (t: string) => Number(parts.find(p => p.type === t)?.value)
  const asUtc = Date.UTC(get('year'), get('month') - 1, get('day'), get('hour') % 24, get('minute'), get('second'))
  return Math.round((asUtc - instant.getTime()) / 60000)
}

const offset = computed(() => {
  const min = offsetMinutes(config.value.timezone, now.value)
  const sign = min >= 0 ? '+' : '−'
  const abs = Math.abs(min)
  const h = String(Math.floor(abs / 60)).padStart(2, '0')
  const m = String(abs % 60).padStart(2, '0')
  return `UTC ${sign}${h}:${m}`
})

const region = computed(() => {
  const tz = config.value.timezone
  if (!tz || tz === 'UTC') return tz === 'UTC' ? 'UTC' : ''
  const parts = tz.split('/')
  return parts[parts.length - 1].replace(/_/g, ' ')
})
</script>

<template>
  <div class="h-full w-full rounded-xl bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 shadow-sm flex flex-col items-center justify-center p-3 select-none">
    <div class="font-display text-3xl md:text-4xl text-gray-900 dark:text-gray-100 tabular-nums">{{ time }}</div>
    <div class="text-xs text-gray-500 mt-1">{{ config.label || date }}</div>
    <div v-if="region || config.timezone" class="text-[10px] text-gray-400 mt-0.5 tabular-nums">
      <span v-if="region">{{ region }} · </span>{{ offset }}
    </div>
  </div>
</template>
