<script setup lang="ts">
import { computed, ref } from 'vue'
import { useStatusStore } from '@/stores/status'
import type { TileStatusHistoryPoint } from '@/types/board'

const props = defineProps<{
  tileId: string
  /** Visual width hint (e.g. tile gridW). Higher = render more samples. */
  resolution?: number
}>()

const status = useStatusStore()

const VIEW_W = 100
const VIEW_H = 24
const PAD_Y = 2

const svgRef = ref<SVGSVGElement | null>(null)

const points = computed<TileStatusHistoryPoint[]>(() => {
  const all = status.history(props.tileId)
  const max = Math.max(20, Math.min(240, (props.resolution ?? 3) * 20))
  return all.length <= max ? all : all.slice(all.length - max)
})

const maxMs = computed(() => {
  const pts = points.value
  if (pts.length === 0) return 1
  return Math.max(1, ...pts.map(p => p.responseTimeMs ?? 0))
})

function pointX(i: number): number {
  const n = points.value.length
  if (n <= 1) return VIEW_W / 2
  return (i / (n - 1)) * VIEW_W
}

function pointY(ms: number): number {
  const usableH = VIEW_H - PAD_Y * 2
  return VIEW_H - PAD_Y - (ms / maxMs.value) * usableH
}

const linePath = computed(() => {
  const pts = points.value
  if (pts.length === 0) return ''
  if (pts.length === 1) {
    // One sample — draw a flat line at that level so the user sees something
    // immediately after the first poll instead of an invisible single point.
    const y = pointY(pts[0].responseTimeMs ?? 0).toFixed(2)
    return `M0 ${y} L${VIEW_W} ${y}`
  }
  let d = ''
  pts.forEach((p, i) => {
    const x = pointX(i)
    const y = pointY(p.responseTimeMs ?? 0)
    d += (i === 0 ? 'M' : 'L') + x.toFixed(2) + ' ' + y.toFixed(2) + ' '
  })
  return d.trim()
})

const downBars = computed(() => {
  const pts = points.value
  if (pts.length === 0) return []
  return pts
    .map((p, i) => ({ p, x: pointX(i) }))
    .filter(b => b.p.status === 'Down')
})

const summary = computed(() => {
  const pts = points.value
  if (pts.length === 0) return ''
  const ups = pts.filter(p => p.status === 'Up').length
  const upPct = ((ups / pts.length) * 100).toFixed(0)
  const upMs = pts.filter(p => p.status === 'Up' && p.responseTimeMs != null)
  const avg = upMs.length > 0
    ? Math.round(upMs.reduce((s, p) => s + (p.responseTimeMs ?? 0), 0) / upMs.length)
    : null
  return avg != null
    ? `${pts.length} samples · ${upPct}% up · avg ${avg} ms · y-axis: ms`
    : `${pts.length} samples · ${upPct}% up`
})

const hover = ref<{ index: number; clientX: number; clientY: number } | null>(null)

function onMove(e: PointerEvent) {
  const rect = svgRef.value?.getBoundingClientRect()
  if (!rect) return
  const pts = points.value
  if (pts.length === 0) return
  const fraction = (e.clientX - rect.left) / rect.width
  const idx = Math.max(0, Math.min(pts.length - 1, Math.round(fraction * (pts.length - 1))))
  hover.value = { index: idx, clientX: e.clientX, clientY: e.clientY }
}

function onLeave() {
  hover.value = null
}

const hoverPoint = computed(() => {
  if (!hover.value) return null
  const pts = points.value
  const i = hover.value.index
  if (i < 0 || i >= pts.length) return null
  const p = pts[i]
  return {
    point: p,
    x: pointX(i),
    y: pointY(p.responseTimeMs ?? 0),
  }
})

const hoverTooltip = computed(() => {
  if (!hover.value || !hoverPoint.value) return null
  const p = hoverPoint.value.point
  const when = new Date(p.checkedUtc).toLocaleTimeString([], {
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
  })
  const ms = p.responseTimeMs != null ? `${p.responseTimeMs} ms` : '—'
  const status = p.status
  return { when, ms, status, clientX: hover.value.clientX, clientY: hover.value.clientY }
})
</script>

<template>
  <div class="relative h-full w-full">
    <svg
      v-if="points.length > 0"
      ref="svgRef"
      :viewBox="`0 0 ${VIEW_W} ${VIEW_H}`"
      preserveAspectRatio="none"
      class="block w-full h-full cursor-crosshair"
      :aria-label="summary"
      @pointermove="onMove"
      @pointerleave="onLeave"
    >
      <title>{{ summary }}</title>
      <rect
        v-for="(bar, i) in downBars"
        :key="`d-${i}`"
        :x="bar.x - 0.6"
        y="0"
        width="1.2"
        :height="VIEW_H"
        class="fill-red-500/30"
      />
      <path
        :d="linePath"
        fill="none"
        stroke="currentColor"
        stroke-width="1.2"
        stroke-linecap="round"
        stroke-linejoin="round"
        vector-effect="non-scaling-stroke"
        class="text-primary-500 dark:text-primary-400"
      />
      <g v-if="hoverPoint">
        <line
          :x1="hoverPoint.x"
          y1="0"
          :x2="hoverPoint.x"
          :y2="VIEW_H"
          stroke="currentColor"
          stroke-width="0.5"
          vector-effect="non-scaling-stroke"
          class="text-gray-400 dark:text-gray-500"
        />
        <circle
          :cx="hoverPoint.x"
          :cy="hoverPoint.y"
          r="1.5"
          :class="hoverPoint.point.status === 'Down'
            ? 'fill-red-500'
            : 'fill-primary-600 dark:fill-primary-400'"
          stroke="white"
          stroke-width="0.5"
          vector-effect="non-scaling-stroke"
        />
      </g>
    </svg>
    <Teleport to="body">
      <div
        v-if="hoverTooltip"
        class="fixed z-50 pointer-events-none px-2 py-1 rounded-md text-xs bg-gray-900/95 text-white shadow-lg whitespace-nowrap"
        :style="{
          left: hoverTooltip.clientX + 'px',
          top: (hoverTooltip.clientY - 12) + 'px',
          transform: 'translate(-50%, -100%)',
        }"
      >
        <div class="font-medium">{{ hoverTooltip.when }} — {{ hoverTooltip.ms }}</div>
        <div class="text-[10px] opacity-80">{{ hoverTooltip.status }}</div>
      </div>
    </Teleport>
  </div>
</template>
