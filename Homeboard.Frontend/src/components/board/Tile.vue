<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useStatusStore } from '@/stores/status'
import { useNow } from '@/composables/useNow'
import StatusSparkline from './StatusSparkline.vue'
import type { TileDto } from '@/types/board'

const props = defineProps<{ tile: TileDto; clickable?: boolean }>()
const status = useStatusStore()
const now = useNow()

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

const dotFillClass = computed(() => {
  if (props.tile.statusType === 'None') return null
  if (!snapshot.value) return 'fill-gray-300 dark:fill-gray-600'
  switch (snapshot.value.status) {
    case 'Up': return 'fill-emerald-500'
    case 'Down': return 'fill-red-500'
    default: return 'fill-gray-300 dark:fill-gray-600'
  }
})

const RING_R = 8
const RING_C = 2 * Math.PI * RING_R

// We cycle the ring against max(tile.statusInterval, FE refresh cadence).
// If a tile is checked more often than the front-end polls, the FE will only
// see updates at its own cadence — using that floor keeps the indicator
// honest and always moving instead of pinning at "due now".
const FRONTEND_POLL_MS = 10_000

const tileSinceMs = computed(() => {
  const snap = snapshot.value
  if (!snap) return null
  const t = new Date(snap.lastCheckedUtc).getTime()
  return Number.isFinite(t) ? Math.max(0, now.value.getTime() - t) : null
})

const effectiveIntervalMs = computed(() => {
  const interval = props.tile.statusInterval * 1000
  return Math.max(interval > 0 ? interval : FRONTEND_POLL_MS, FRONTEND_POLL_MS)
})

// Continuous two-phase animation. The ring's position in a 2-phase cycle is
// driven directly by `elapsed / interval`, so it keeps moving every useNow
// tick regardless of the FE refresh cadence (no plateau between polls). When
// a new snapshot lands, history.length flips parity which shifts the position
// by exactly one phase — and lastCheckedUtc advances by ~interval, dropping
// elapsed by the same amount. The two changes cancel, so the position is
// continuous across snapshot updates.
const ringDashState = computed(() => {
  const snap = snapshot.value
  if (!snap) return { array: `0 ${RING_C}`, offset: 0 }
  const interval = effectiveIntervalMs.value
  const elapsed = Math.max(0, now.value.getTime() - new Date(snap.lastCheckedUtc).getTime())
  const parity = status.history(props.tile.id).length % 2
  const raw = elapsed / interval + (parity === 1 ? 1 : 0)
  const position = ((raw % 2) + 2) % 2 // 0..2
  if (position < 1) {
    // Phase A: head advancing clockwise from 12 o'clock; tail stays at 0.
    const L = position * RING_C
    return { array: `${L} ${RING_C - L}`, offset: 0 }
  }
  // Phase B: tail catching up to the head (also clockwise), shrinking the arc.
  const within = position - 1
  const S = within * RING_C
  const L = RING_C - S
  return { array: `${L} ${S}`, offset: -S }
})

const nextCheckIn = computed(() => {
  const elapsed = tileSinceMs.value
  if (elapsed == null) return null
  const remainder = effectiveIntervalMs.value - (elapsed % effectiveIntervalMs.value)
  return Math.max(0, Math.ceil(remainder / 1000))
})

const isOverdue = computed(() => {
  const elapsed = tileSinceMs.value
  if (elapsed == null) return false
  const interval = props.tile.statusInterval * 1000
  return interval > 0 && elapsed > interval + 2000
})

const showStatus = computed(() => props.tile.statusType !== 'None')

const dotTitle = computed(() => {
  if (props.tile.statusType === 'None') return undefined
  if (!snapshot.value) return 'No check yet'
  const checked = new Date(snapshot.value.lastCheckedUtc).toLocaleString()
  const time = snapshot.value.responseTimeMs != null ? ` (${snapshot.value.responseTimeMs} ms)` : ''
  const note = snapshot.value.note ? ` — ${snapshot.value.note}` : ''
  const next = nextCheckIn.value
  const nextStr = isOverdue.value
    ? ' · check is due'
    : (next != null && next > 0) ? ` · next in ${next}s` : ''
  return `${snapshot.value.status} at ${checked}${time}${note}${nextStr}`
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
    <svg
      v-if="showStatus"
      class="absolute top-1.5 right-1.5 w-5 h-5 overflow-visible"
      viewBox="0 0 20 20"
      aria-hidden="true"
    >
      <title>{{ dotTitle }}</title>
      <g transform="rotate(-90 10 10)">
        <circle cx="10" cy="10" r="8" fill="none" stroke-width="1.5" class="stroke-gray-200 dark:stroke-gray-700" />
        <circle
          cx="10" cy="10" r="8"
          fill="none"
          stroke="currentColor"
          stroke-width="1.5"
          stroke-linecap="round"
          :stroke-dasharray="ringDashState.array"
          :stroke-dashoffset="ringDashState.offset"
          style="transition: stroke-dasharray 700ms linear, stroke-dashoffset 700ms linear;"
          class="text-gray-500 dark:text-gray-400"
        />
      </g>
      <circle cx="10" cy="10" r="4.5" :class="dotFillClass!" />
    </svg>
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
    <div
      v-if="tile.statusType !== 'None'"
      class="absolute inset-x-2 bottom-1 h-4"
    >
      <StatusSparkline :tile-id="tile.id" :resolution="tile.gridW" />
    </div>
  </component>
</template>
