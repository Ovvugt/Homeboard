<script setup lang="ts">
import ClockWidget from '@/components/widgets/ClockWidget.vue'
import WeatherWidget from '@/components/widgets/WeatherWidget.vue'
import type { WidgetDto } from '@/types/board'

defineProps<{ widget: WidgetDto }>()
</script>

<template>
  <ClockWidget v-if="widget.type === 'Clock'" :widget="widget" />
  <WeatherWidget v-else-if="widget.type === 'Weather'" :widget="widget" />
  <div v-else class="h-full w-full rounded-xl bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 shadow-sm flex items-center justify-center text-gray-500">
    Unknown: {{ widget.type }}
  </div>
</template>
