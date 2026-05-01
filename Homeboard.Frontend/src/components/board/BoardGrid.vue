<script setup lang="ts">
import { computed, onBeforeUnmount, ref, watch } from 'vue'
import { GridLayout, GridItem } from 'grid-layout-plus'
import { TrashIcon } from '@heroicons/vue/24/outline'
import { boardsApi } from '@/api/boards'
import Tile from './Tile.vue'
import WidgetHost from './WidgetHost.vue'
import type { BoardDetail, LayoutItem, LayoutItemKind, TileDto, WidgetDto } from '@/types/board'

const props = defineProps<{ board: BoardDetail; editable: boolean }>()
const emit = defineEmits<{
  'edit-tile': [tileId: string]
  'edit-widget': [widgetId: string]
  'delete-tile': [tileId: string]
  'delete-widget': [widgetId: string]
}>()

interface GridLayoutItem {
  i: string
  x: number
  y: number
  w: number
  h: number
}

function buildLayout(b: BoardDetail): GridLayoutItem[] {
  return [
    ...b.tiles.map(t => ({ i: `t:${t.id}`, x: t.gridX, y: t.gridY, w: t.gridW, h: t.gridH })),
    ...b.widgets.map(w => ({ i: `w:${w.id}`, x: w.gridX, y: w.gridY, w: w.gridW, h: w.gridH })),
  ]
}

const layout = ref<GridLayoutItem[]>(buildLayout(props.board))
watch(() => props.board, b => { layout.value = buildLayout(b) })

const tilesById = computed(() => {
  const m = new Map<string, TileDto>()
  for (const t of props.board.tiles) m.set(t.id, t)
  return m
})
const widgetsById = computed(() => {
  const m = new Map<string, WidgetDto>()
  for (const w of props.board.widgets) m.set(w.id, w)
  return m
})

function parseKey(i: string): { kind: LayoutItemKind; id: string } {
  const [prefix, id] = i.split(':')
  return { kind: prefix === 't' ? 'Tile' : 'Widget', id }
}

let saveTimer: ReturnType<typeof setTimeout> | null = null
async function persistLayout(items: GridLayoutItem[]) {
  const payload: LayoutItem[] = items.map(it => {
    const { kind, id } = parseKey(it.i)
    return { id, kind, gridX: it.x, gridY: it.y, gridW: it.w, gridH: it.h }
  })
  try {
    await boardsApi.saveLayout(props.board.id, { items: payload })
  } catch (e) {
    console.error('Failed to save layout', e)
  }
}

function onLayoutUpdated(newLayout: GridLayoutItem[]) {
  if (!props.editable) return
  if (saveTimer) clearTimeout(saveTimer)
  saveTimer = setTimeout(() => persistLayout(newLayout), 500)
}

// --- Drag-to-trash + click-vs-drag detection ---

interface DragInfo {
  itemKey: string
  startX: number
  startY: number
  currentX: number
  currentY: number
  overTrash: boolean
}

const drag = ref<DragInfo | null>(null)
const trashRef = ref<HTMLElement | null>(null)
const crumblingKey = ref<string | null>(null)

function onPointerDown(e: PointerEvent, itemKey: string) {
  if (!props.editable) return
  drag.value = {
    itemKey,
    startX: e.clientX,
    startY: e.clientY,
    currentX: e.clientX,
    currentY: e.clientY,
    overTrash: false,
  }
  document.addEventListener('pointermove', onPointerMove)
  document.addEventListener('pointerup', onPointerUp, { once: true })
  document.addEventListener('pointercancel', onPointerCancel, { once: true })
}

function onPointerMove(e: PointerEvent) {
  if (!drag.value) return
  drag.value.currentX = e.clientX
  drag.value.currentY = e.clientY
  if (trashRef.value) {
    const r = trashRef.value.getBoundingClientRect()
    drag.value.overTrash =
      e.clientX >= r.left && e.clientX <= r.right &&
      e.clientY >= r.top && e.clientY <= r.bottom
  }
}

function cleanupDocListeners() {
  document.removeEventListener('pointermove', onPointerMove)
}

function onPointerUp(e: PointerEvent) {
  cleanupDocListeners()
  const info = drag.value
  drag.value = null
  if (!info) return

  const dx = e.clientX - info.startX
  const dy = e.clientY - info.startY
  const dist = Math.hypot(dx, dy)

  // Was a click? (no meaningful movement) — open the editor
  if (dist < 5) {
    const { kind, id } = parseKey(info.itemKey)
    if (kind === 'Tile') emit('edit-tile', id)
    else emit('edit-widget', id)
    return
  }

  // Was dragged onto trash zone?
  if (info.overTrash) {
    crumbleAndDelete(info.itemKey)
  }
}

function onPointerCancel() {
  cleanupDocListeners()
  drag.value = null
}

function crumbleAndDelete(itemKey: string) {
  crumblingKey.value = itemKey
  setTimeout(() => {
    const { kind, id } = parseKey(itemKey)
    if (kind === 'Tile') emit('delete-tile', id)
    else emit('delete-widget', id)
    crumblingKey.value = null
  }, 380)
}

onBeforeUnmount(cleanupDocListeners)

const trashActive = computed(() => !!drag.value?.overTrash)
const dragging = computed(() => !!drag.value)
</script>

<template>
  <div class="p-6 pb-32 relative">
    <div class="text-sm text-gray-500 mb-3">
      {{ layout.length }} item(s) · {{ board.gridColumns }} columns · {{ editable ? 'edit mode' : 'view mode' }}
    </div>
    <GridLayout
      v-model:layout="layout"
      :col-num="board.gridColumns"
      :row-height="80"
      :margin="[12, 12]"
      :is-draggable="editable"
      :is-resizable="editable"
      :vertical-compact="false"
      :prevent-collision="true"
      :use-css-transforms="true"
      @layout-updated="onLayoutUpdated"
    >
      <GridItem
        v-for="item in layout"
        :key="item.i"
        :x="item.x"
        :y="item.y"
        :w="item.w"
        :h="item.h"
        :i="item.i"
        drag-allow-from=".drag-handle"
      >
        <div
          class="relative h-full w-full"
          :class="{ 'tile-crumble': crumblingKey === item.i }"
        >
          <template v-if="parseKey(item.i).kind === 'Tile'">
            <Tile
              v-if="tilesById.get(parseKey(item.i).id)"
              :tile="tilesById.get(parseKey(item.i).id)!"
              :clickable="!editable"
            />
          </template>
          <template v-else>
            <WidgetHost
              v-if="widgetsById.get(parseKey(item.i).id)"
              :widget="widgetsById.get(parseKey(item.i).id)!"
            />
          </template>
          <div
            v-if="editable"
            class="drag-handle absolute inset-0 cursor-move bg-primary-500/0 hover:bg-primary-500/5 rounded-xl ring-2 ring-primary-400/0 hover:ring-primary-400/40 transition pointer-events-auto"
            @pointerdown="onPointerDown($event, item.i)"
          />
        </div>
      </GridItem>
    </GridLayout>

    <Transition name="trash">
      <div
        v-if="editable"
        ref="trashRef"
        class="trash-zone fixed bottom-6 left-1/2 -translate-x-1/2 z-30 pointer-events-none flex items-center gap-2 px-5 py-3 rounded-full border-2 border-dashed select-none"
        :class="trashActive
          ? 'bg-red-500 text-white border-red-400 scale-110'
          : dragging
            ? 'bg-white/95 dark:bg-gray-900/95 text-red-600 border-red-400'
            : 'bg-white/90 dark:bg-gray-900/90 text-gray-500 border-gray-300 dark:border-gray-700'"
      >
        <TrashIcon class="w-5 h-5" />
        <span class="text-sm font-medium">
          {{ trashActive ? 'Release to delete' : 'Drag here to delete' }}
        </span>
      </div>
    </Transition>
  </div>
</template>

<style>
.vgl-layout {
  --vgl-placeholder-bg: rgb(13 148 136);
  --vgl-placeholder-opacity: 25%;
}
.vgl-item--placeholder { border-radius: 0.75rem; }

.trash-zone { transition: background-color 0.15s, border-color 0.15s, color 0.15s, transform 0.18s ease, opacity 0.18s ease; }
.trash-enter-from, .trash-leave-to { opacity: 0; transform: translate(-50%, 16px); }
.trash-enter-active, .trash-leave-active { transition: opacity 0.18s ease, transform 0.18s ease; }

.tile-crumble {
  animation: crumble 0.38s ease forwards;
  pointer-events: none;
}
@keyframes crumble {
  0%   { transform: scale(1) rotate(0deg); opacity: 1; filter: blur(0); }
  60%  { transform: scale(0.6) rotate(-12deg); opacity: 0.6; filter: blur(1px); }
  100% { transform: scale(0.1) rotate(40deg) translateY(40px); opacity: 0; filter: blur(3px); }
}

.group:hover .opacity-0.group-hover\:opacity-100 { opacity: 1; }
</style>
