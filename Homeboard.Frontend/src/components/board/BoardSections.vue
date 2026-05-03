<script setup lang="ts">
import { computed, onBeforeUnmount, ref } from 'vue'
import { TrashIcon } from '@heroicons/vue/24/outline'
import { boardsApi } from '@/api/boards'
import { sectionsApi } from '@/api/sections'
import SectionView, { type SectionContent } from './SectionView.vue'
import { findFreeSpot, type GridRect } from '@/utils/placement'
import type { BoardDetail, LayoutItem, SectionDto, TileDto, WidgetDto } from '@/types/board'

interface SectionItem { i: string; x: number; y: number; w: number; h: number }

const props = defineProps<{ board: BoardDetail; editable: boolean }>()
const emit = defineEmits<{
  'edit-tile': [tileId: string]
  'edit-widget': [widgetId: string]
  'delete-tile': [tileId: string]
  'delete-widget': [widgetId: string]
  'add-tile': [sectionId: string]
  'add-widget': [sectionId: string]
  'changed': []
}>()

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

const rootSection = computed<SectionDto | null>(() => {
  return props.board.sections.find(s => s.parentId === null) ?? null
})

function childrenOf(parentId: string | null): SectionDto[] {
  return props.board.sections
    .filter(s => s.parentId === parentId)
    .slice()
    .sort((a, b) => a.sortOrder - b.sortOrder)
}

// One pass over tiles+widgets, grouped by their resolved section id (null → root).
const itemsBySection = computed<Map<string, SectionContent[]>>(() => {
  const m = new Map<string, SectionContent[]>()
  for (const s of props.board.sections) m.set(s.id, [])
  const rootId = rootSection.value?.id
  const push = (id: string, item: SectionContent) => {
    let arr = m.get(id)
    if (!arr) { arr = []; m.set(id, arr) }
    arr.push(item)
  }
  for (const t of props.board.tiles) {
    const sid = t.sectionId ?? rootId
    if (!sid) continue
    push(sid, { kind: 'Tile', id: t.id, gridX: t.gridX, gridY: t.gridY, gridW: t.gridW, gridH: t.gridH })
  }
  for (const w of props.board.widgets) {
    const sid = w.sectionId ?? rootId
    if (!sid) continue
    push(sid, { kind: 'Widget', id: w.id, gridX: w.gridX, gridY: w.gridY, gridW: w.gridW, gridH: w.gridH })
  }
  return m
})

function itemsForSection(sectionId: string): SectionContent[] {
  return itemsBySection.value.get(sectionId) ?? []
}

// Track per-section pending layout saves so cross-section moves can cancel them.
const saveTimers = new Map<string, ReturnType<typeof setTimeout>>()

function parseKey(i: string): { kind: 'Tile' | 'Widget'; id: string } {
  const [prefix, id] = i.split(':')
  return { kind: prefix === 't' ? 'Tile' : 'Widget', id }
}

function onLayoutUpdated(sectionId: string, items: SectionItem[]) {
  if (!props.editable) return
  // Optimistic local mutation so when edit mode exits, SectionView rebuilds
  // its layout from up-to-date store state instead of snapping back to the
  // last server-loaded values.
  applyLayoutToStore(sectionId, items)
  const existing = saveTimers.get(sectionId)
  if (existing) clearTimeout(existing)
  saveTimers.set(sectionId, setTimeout(() => persistSection(sectionId, items), 500))
}

function applyLayoutToStore(sectionId: string, items: SectionItem[]) {
  for (const it of items) {
    const { kind, id } = parseKey(it.i)
    if (kind === 'Tile') {
      const t = props.board.tiles.find(x => x.id === id)
      if (t) {
        t.gridX = it.x; t.gridY = it.y; t.gridW = it.w; t.gridH = it.h
        t.sectionId = sectionId
      }
    } else {
      const w = props.board.widgets.find(x => x.id === id)
      if (w) {
        w.gridX = it.x; w.gridY = it.y; w.gridW = it.w; w.gridH = it.h
        w.sectionId = sectionId
      }
    }
  }
}

async function persistSection(sectionId: string, items: SectionItem[]) {
  const payload: LayoutItem[] = items.map(it => {
    const { kind, id } = parseKey(it.i)
    return { id, kind, sectionId, gridX: it.x, gridY: it.y, gridW: it.w, gridH: it.h }
  })
  if (payload.length === 0) return
  try {
    await boardsApi.saveLayout(props.board.id, { items: payload })
  } catch (e) {
    console.error('Failed to save layout', e)
  }
}

// --- Pointer-driven drag detection (trash + cross-section move) ---

interface DragInfo {
  itemKey: string
  sourceSectionId: string
  startX: number
  startY: number
  currentX: number
  currentY: number
  overTrash: boolean
  overSectionId: string | null
}

const drag = ref<DragInfo | null>(null)
const trashRef = ref<HTMLElement | null>(null)
const crumblingKey = ref<string | null>(null)

function onPointerDown(sourceSectionId: string, itemKey: string, e: PointerEvent) {
  if (!props.editable) return
  drag.value = {
    itemKey,
    sourceSectionId,
    startX: e.clientX,
    startY: e.clientY,
    currentX: e.clientX,
    currentY: e.clientY,
    overTrash: false,
    overSectionId: null,
  }
  document.addEventListener('pointermove', onPointerMove)
  document.addEventListener('pointerup', onPointerUp, { once: true })
  document.addEventListener('pointercancel', onPointerCancel, { once: true })
}

function findSectionAtPoint(x: number, y: number): string | null {
  // Hit-test against every rendered .section-block; the deepest match wins.
  const blocks = document.querySelectorAll<HTMLElement>('.section-block[data-section-id]')
  let match: { id: string; area: number } | null = null
  for (const el of blocks) {
    const r = el.getBoundingClientRect()
    if (x >= r.left && x <= r.right && y >= r.top && y <= r.bottom) {
      const area = r.width * r.height
      if (!match || area < match.area) {
        match = { id: el.dataset.sectionId!, area }
      }
    }
  }
  return match?.id ?? null
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
  drag.value.overSectionId = findSectionAtPoint(e.clientX, e.clientY)
}

function cleanupDocListeners() {
  document.removeEventListener('pointermove', onPointerMove)
}

async function onPointerUp(e: PointerEvent) {
  cleanupDocListeners()
  const info = drag.value
  drag.value = null
  if (!info) return

  const dx = e.clientX - info.startX
  const dy = e.clientY - info.startY
  const dist = Math.hypot(dx, dy)

  // Click without movement → open editor.
  if (dist < 5) {
    const { kind, id } = parseKey(info.itemKey)
    if (kind === 'Tile') emit('edit-tile', id)
    else emit('edit-widget', id)
    return
  }

  if (info.overTrash) {
    crumbleAndDelete(info.itemKey)
    return
  }

  // Cross-section move: drop landed in a different section.
  if (info.overSectionId && info.overSectionId !== info.sourceSectionId) {
    // Cancel any pending in-section save for the source — we'll rewrite both sections.
    const pending = saveTimers.get(info.sourceSectionId)
    if (pending) {
      clearTimeout(pending)
      saveTimers.delete(info.sourceSectionId)
    }
    await moveItemToSection(info.itemKey, info.overSectionId)
  }
}

async function moveItemToSection(itemKey: string, targetSectionId: string) {
  const { kind, id } = parseKey(itemKey)
  // Place the moved item at the next free spot in the target section.
  const occupants: GridRect[] = itemsForSection(targetSectionId).map(it => ({
    gridX: it.gridX, gridY: it.gridY, gridW: it.gridW, gridH: it.gridH,
  }))
  const original = kind === 'Tile' ? tilesById.value.get(id) : widgetsById.value.get(id)
  const w = original?.gridW ?? 2
  const h = original?.gridH ?? 2
  const spot = findFreeSpot(occupants, props.board.gridColumns, w, h)
  try {
    await boardsApi.saveLayout(props.board.id, {
      items: [{ id, kind, sectionId: targetSectionId, gridX: spot.x, gridY: spot.y, gridW: w, gridH: h }],
    })
    emit('changed')
  } catch (e) {
    console.error('Failed to move item across sections', e)
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

onBeforeUnmount(() => {
  cleanupDocListeners()
  for (const t of saveTimers.values()) clearTimeout(t)
  saveTimers.clear()
})

const trashActive = computed(() => !!drag.value?.overTrash)
const dragging = computed(() => !!drag.value)

// --- Section management ---

async function onAddSubsection(parentId: string) {
  const name = prompt('Section name')
  if (name === null) return
  try {
    await sectionsApi.create({ boardId: props.board.id, parentId, name: name.trim() || null })
    emit('changed')
  } catch (e) {
    console.error('Failed to create section', e)
  }
}

async function onRenameSection(sectionId: string, name: string) {
  const section = props.board.sections.find(s => s.id === sectionId)
  if (!section) return
  try {
    await sectionsApi.update(sectionId, {
      name: name || null,
      parentId: section.parentId,
      sortOrder: section.sortOrder,
      collapsed: section.collapsed,
    })
    emit('changed')
  } catch (e) {
    console.error('Failed to rename section', e)
  }
}

async function onToggleCollapse(sectionId: string) {
  const section = props.board.sections.find(s => s.id === sectionId)
  if (!section) return
  try {
    await sectionsApi.update(sectionId, {
      name: section.name,
      parentId: section.parentId,
      sortOrder: section.sortOrder,
      collapsed: !section.collapsed,
    })
    emit('changed')
  } catch (e) {
    console.error('Failed to toggle collapse', e)
  }
}

async function onDeleteSection(sectionId: string) {
  const section = props.board.sections.find(s => s.id === sectionId)
  if (!section) return
  if (!confirm(`Delete section "${section.name ?? 'Untitled'}"? Items inside move to the root section.`)) return
  try {
    await sectionsApi.delete(sectionId)
    emit('changed')
  } catch (e) {
    console.error('Failed to delete section', e)
  }
}
</script>

<template>
  <div class="p-6 pb-32 relative">
    <div class="text-sm text-gray-500 mb-3">
      {{ board.tiles.length + board.widgets.length }} item(s) · {{ board.sections.length }} section(s) ·
      {{ board.gridColumns }} columns · {{ editable ? 'edit mode' : 'view mode' }}
    </div>

    <SectionView
      v-if="rootSection"
      :board="board"
      :section="rootSection"
      :editable="editable"
      :tiles-by-id="tilesById"
      :widgets-by-id="widgetsById"
      :items-by-section="itemsBySection"
      :child-sections="childrenOf(rootSection.id)"
      :depth="0"
      :crumbling-key="crumblingKey"
      @layout-updated="onLayoutUpdated"
      @pointer-down="onPointerDown"
      @add-tile="sid => emit('add-tile', sid)"
      @add-widget="sid => emit('add-widget', sid)"
      @add-subsection="onAddSubsection"
      @rename-section="onRenameSection"
      @toggle-collapse="onToggleCollapse"
      @delete-section="onDeleteSection"
    />

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
</style>
