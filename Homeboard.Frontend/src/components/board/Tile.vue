<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useStatusStore } from '@/stores/status'
import type { TileDto } from '@/types/board'

const props = defineProps<{ tile: TileDto; clickable?: boolean }>()
const status = useStatusStore()

const initials = computed(() => {
  const parts = props.tile.name.trim().split(/\s+/).slice(0, 2)
  return parts.map(p => p[0]?.toUpperCase() ?? '').join('') || '?'
})

const autoIconUrl = computed(() => {
  if (props.tile.iconKind !== 'Url') return null
  if (props.tile.iconUrl) return props.tile.iconUrl
  if (!props.tile.url) return null
  return `/api/icons?url=${encodeURIComponent(props.tile.url)}`
})

// If the auto-fetch fails (404) we want to fall back to initials.
const iconFailed = ref(false)
watch(autoIconUrl, () => { iconFailed.value = false })

const showImage = computed(() => !!autoIconUrl.value && !iconFailed.value)

const snapshot = computed(() => status.get(props.tile.id))

const dotClass = computed(() => {
  if (props.tile.statusType === 'None') return null
  if (!snapshot.value) return 'bg-gray-300 dark:bg-gray-600'
  switch (snapshot.value.status) {
    case 'Up': return 'bg-emerald-500'
    case 'Down': return 'bg-red-500'
    default: return 'bg-gray-300 dark:bg-gray-600'
  }
})

const dotTitle = computed(() => {
  if (props.tile.statusType === 'None') return undefined
  if (!snapshot.value) return 'No check yet'
  const checked = new Date(snapshot.value.lastCheckedUtc).toLocaleString()
  const time = snapshot.value.responseTimeMs != null ? ` (${snapshot.value.responseTimeMs} ms)` : ''
  const note = snapshot.value.note ? ` — ${snapshot.value.note}` : ''
  return `${snapshot.value.status} at ${checked}${time}${note}`
})
</script>

<template>
  <component
    :is="clickable ? 'a' : 'div'"
    :href="clickable ? tile.url : undefined"
    :target="clickable ? '_blank' : undefined"
    :rel="clickable ? 'noopener noreferrer' : undefined"
    class="group relative h-full w-full rounded-xl bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 shadow-sm hover:shadow-md hover:border-primary-400 dark:hover:border-primary-500 transition flex flex-col items-center justify-center text-center p-3 gap-2 select-none"
  >
    <span
      v-if="dotClass"
      class="absolute top-2 right-2 w-2.5 h-2.5 rounded-full ring-2 ring-white dark:ring-gray-800"
      :class="dotClass"
      :title="dotTitle"
    />
    <div
      class="w-12 h-12 rounded-lg overflow-hidden flex items-center justify-center font-semibold"
      :class="showImage
        ? ''
        : 'bg-primary-100 dark:bg-primary-900/40 text-primary-700 dark:text-primary-300'"
    >
      <img
        v-if="showImage"
        :src="autoIconUrl!"
        :alt="tile.name"
        loading="lazy"
        decoding="async"
        class="w-full h-full object-contain"
        @error="iconFailed = true"
      />
      <span v-else>{{ initials }}</span>
    </div>
    <div class="text-sm font-medium text-gray-800 dark:text-gray-100 truncate w-full" :title="tile.name">{{ tile.name }}</div>
  </component>
</template>
