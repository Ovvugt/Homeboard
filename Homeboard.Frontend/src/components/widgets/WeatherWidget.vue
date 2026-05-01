<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { weatherApi, type WeatherDto } from '@/api/weather'
import type { WidgetDto } from '@/types/board'

const props = defineProps<{ widget: WidgetDto }>()

interface WeatherConfig {
  latitude?: number
  longitude?: number
  label?: string
}

const config = computed<WeatherConfig>(() => {
  try { return JSON.parse(props.widget.configJson) as WeatherConfig } catch { return {} }
})

const weather = ref<WeatherDto | null>(null)
const error = ref<string | null>(null)
let timer: ReturnType<typeof setInterval> | null = null

async function load() {
  if (config.value.latitude == null || config.value.longitude == null) {
    error.value = 'Set latitude and longitude'
    return
  }
  try {
    weather.value = await weatherApi.get(config.value.latitude, config.value.longitude)
    error.value = null
  } catch (e) {
    error.value = e instanceof Error ? e.message : String(e)
  }
}

onMounted(() => {
  load()
  timer = setInterval(load, 10 * 60 * 1000)
})
onBeforeUnmount(() => { if (timer) clearInterval(timer) })
watch(() => props.widget.configJson, load)

const codeMap: Record<number, string> = {
  0: 'Clear', 1: 'Mostly clear', 2: 'Partly cloudy', 3: 'Overcast',
  45: 'Fog', 48: 'Rime fog', 51: 'Drizzle', 53: 'Drizzle', 55: 'Drizzle',
  61: 'Rain', 63: 'Rain', 65: 'Heavy rain', 71: 'Snow', 73: 'Snow', 75: 'Heavy snow',
  77: 'Snow grains', 80: 'Showers', 81: 'Showers', 82: 'Heavy showers',
  85: 'Snow showers', 86: 'Snow showers', 95: 'Thunderstorm', 96: 'Thunderstorm', 99: 'Thunderstorm',
}

const description = computed(() => {
  if (!weather.value || weather.value.weatherCode == null) return ''
  return codeMap[weather.value.weatherCode] ?? `Code ${weather.value.weatherCode}`
})
</script>

<template>
  <div class="h-full w-full rounded-xl bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 shadow-sm p-4 flex flex-col select-none">
    <div class="text-xs uppercase tracking-wider text-gray-500">{{ config.label || 'Weather' }}</div>
    <div v-if="error" class="text-sm text-red-600 mt-2">{{ error }}</div>
    <template v-else-if="weather">
      <div class="font-display text-3xl md:text-4xl text-gray-900 dark:text-gray-100 tabular-nums mt-1">
        {{ Math.round(weather.temperatureC) }}°C
      </div>
      <div class="text-sm text-gray-600 dark:text-gray-300">{{ description }}</div>
      <div class="text-xs text-gray-500 mt-auto">
        <span v-if="weather.apparentTemperatureC != null">Feels {{ Math.round(weather.apparentTemperatureC) }}° · </span>
        <span v-if="weather.windSpeedKmh != null">{{ Math.round(weather.windSpeedKmh) }} km/h</span>
      </div>
    </template>
    <div v-else class="text-sm text-gray-500">Loading…</div>
  </div>
</template>
