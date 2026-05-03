<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { GridLayout, GridItem } from 'grid-layout-plus'
import { ChevronDownIcon, ChevronRightIcon, PencilSquareIcon, PlusIcon, TrashIcon } from '@heroicons/vue/24/outline'
import Tile from './Tile.vue'
import WidgetHost from './WidgetHost.vue'
import { findEndSpot, type GridRect } from '@/utils/placement'
import type { BoardDetail, SectionDto, TileDto, WidgetDto } from '@/types/board'

interface SectionItem {
  i: string             // grid-layout key, "t:<id>" | "w:<id>" | "add:<sectionId>"
  x: number
  y: number
  w: number
  h: number
}

export interface SectionContent {
  kind: 'Tile' | 'Widget'
  id: string
  gridX: number
  gridY: number
  gridW: number
  gridH: number
}

const props = defineProps<{
  board: BoardDetail
  section: SectionDto
  editable: boolean
  tilesById: Map<string, TileDto>
  widgetsById: Map<string, WidgetDto>
  itemsBySection: Map<string, SectionContent[]>
  childSections: SectionDto[]
  depth: number
  crumblingKey: string | null
}>()

const emit = defineEmits<{
  'layout-updated': [sectionId: string, items: SectionItem[]]
  'pointer-down': [sectionId: string, itemKey: string, ev: PointerEvent]
  'add-tile': [sectionId: string]
  'add-widget': [sectionId: string]
  'add-subsection': [parentId: string]
  'rename-section': [sectionId: string, name: string]
  'toggle-collapse': [sectionId: string]
  'delete-section': [sectionId: string]
}>()

const isRoot = computed(() => props.section.parentId === null)
const showHeading = computed(() => !isRoot.value || props.editable)

const sectionItems = computed<SectionContent[]>(() => props.itemsBySection.get(props.section.id) ?? [])

const layout = ref<SectionItem[]>([])

const addSpot = ref<{ x: number; y: number; w: number; h: number }>({ x: 0, y: 0, w: 2, h: 2 })

function rebuild() {
  layout.value = sectionItems.value.map(it => ({
    i: `${it.kind === 'Tile' ? 't' : 'w'}:${it.id}`,
    x: it.gridX,
    y: it.gridY,
    w: it.gridW,
    h: it.gridH,
  }))
  if (props.editable && !props.section.collapsed) {
    const occupants: GridRect[] = layout.value.map(b => ({ gridX: b.x, gridY: b.y, gridW: b.w, gridH: b.h }))
    const w = 2, h = 2
    const spot = findEndSpot(occupants, props.board.gridColumns, w, h)
    addSpot.value = { x: spot.x, y: spot.y, w, h }
  }
}

watch([
  () => props.editable,
  () => props.section.collapsed,
  () => sectionItems.value,
], rebuild, { immediate: true, deep: true })

function onLayoutUpdated(items: SectionItem[]) {
  // The add button is a static GridItem that's NOT in our `layout` ref (we render it
  // separately below). Filter it out defensively in case grid-layout-plus echoes it.
  emit('layout-updated', props.section.id, items.filter(it => !it.i.startsWith('add:')))
}

function onAddClick() {
  emit('add-tile', props.section.id)
}

function onPointerDown(ev: PointerEvent, key: string) {
  if (!props.editable) return
  emit('pointer-down', props.section.id, key, ev)
}

function parseKey(i: string): { kind: 'Tile' | 'Widget'; id: string } {
  const [prefix, id] = i.split(':')
  return { kind: prefix === 't' ? 'Tile' : 'Widget', id }
}

const renaming = ref(false)
const draftName = ref('')

function startRename() {
  draftName.value = props.section.name ?? ''
  renaming.value = true
}

function commitRename() {
  emit('rename-section', props.section.id, draftName.value.trim())
  renaming.value = false
}

function cancelRename() {
  renaming.value = false
}

const indentPx = computed(() => Math.min(props.depth, 4) * 16)
</script>

<template>
  <section
    class="section-block"
    :data-section-id="section.id"
    :style="{ marginLeft: `${indentPx}px` }"
  >
    <div
      v-if="showHeading"
      class="flex items-center gap-2 mb-2 mt-4 first:mt-0 group"
    >
      <button
        v-if="!isRoot"
        type="button"
        class="text-gray-500 hover:text-gray-800 dark:hover:text-gray-200"
        @click="emit('toggle-collapse', section.id)"
        :title="section.collapsed ? 'Expand' : 'Collapse'"
      >
        <ChevronDownIcon v-if="!section.collapsed" class="w-4 h-4" />
        <ChevronRightIcon v-else class="w-4 h-4" />
      </button>

      <template v-if="renaming">
        <input
          v-model="draftName"
          class="px-2 py-1 text-base rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800"
          autofocus
          @keydown.enter.prevent="commitRename"
          @keydown.escape.prevent="cancelRename"
          @blur="commitRename"
        />
      </template>
      <template v-else>
        <h2
          class="font-display text-lg text-gray-800 dark:text-gray-200"
          :class="{ italic: !section.name && !isRoot, 'text-gray-400': !section.name }"
        >
          {{ section.name ?? (isRoot ? 'Ungrouped' : 'Untitled section') }}
        </h2>
      </template>

      <template v-if="editable">
        <button
          v-if="!isRoot"
          type="button"
          class="opacity-0 group-hover:opacity-100 text-gray-500 hover:text-primary-600 transition"
          @click="startRename"
          title="Rename"
        >
          <PencilSquareIcon class="w-4 h-4" />
        </button>
        <button
          type="button"
          class="opacity-0 group-hover:opacity-100 text-gray-500 hover:text-primary-600 transition inline-flex items-center gap-1 text-xs"
          @click="emit('add-subsection', section.id)"
          title="Add subsection"
        >
          <PlusIcon class="w-4 h-4" /> subsection
        </button>
        <button
          v-if="!isRoot"
          type="button"
          class="opacity-0 group-hover:opacity-100 text-gray-500 hover:text-red-600 transition ml-auto"
          @click="emit('delete-section', section.id)"
          title="Delete section (items move to root)"
        >
          <TrashIcon class="w-4 h-4" />
        </button>
      </template>
    </div>

    <div v-if="!section.collapsed">
      <GridLayout
        v-model:layout="layout"
        :col-num="board.gridColumns"
        :row-height="80"
        :margin="[12, 12]"
        :is-draggable="editable"
        :is-resizable="editable"
        :vertical-compact="true"
        :prevent-collision="false"
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
              class="drag-handle edit-frame absolute inset-0 cursor-move rounded-xl pointer-events-auto"
              @pointerdown="onPointerDown($event, item.i)"
            />
          </div>
        </GridItem>
        <GridItem
          v-if="editable"
          :i="`add:${section.id}`"
          :x="addSpot.x"
          :y="addSpot.y"
          :w="addSpot.w"
          :h="addSpot.h"
          :static="true"
        >
          <button
            type="button"
            class="add-slot h-full w-full rounded-xl border-2 border-dashed border-gray-300 dark:border-gray-600 text-gray-400 hover:text-primary-600 hover:border-primary-400 flex flex-col items-center justify-center gap-1 transition"
            @click="onAddClick"
          >
            <PlusIcon class="w-6 h-6" />
            <span class="text-xs">Add tile</span>
          </button>
        </GridItem>
      </GridLayout>
    </div>

    <SectionView
      v-for="child in childSections"
      :key="child.id"
      :board="board"
      :section="child"
      :editable="editable"
      :tiles-by-id="tilesById"
      :widgets-by-id="widgetsById"
      :items-by-section="itemsBySection"
      :child-sections="board.sections.filter(s => s.parentId === child.id)"
      :depth="depth + 1"
      :crumbling-key="crumblingKey"
      @layout-updated="(sid, items) => emit('layout-updated', sid, items)"
      @pointer-down="(sid, key, ev) => emit('pointer-down', sid, key, ev)"
      @add-tile="sid => emit('add-tile', sid)"
      @add-widget="sid => emit('add-widget', sid)"
      @add-subsection="pid => emit('add-subsection', pid)"
      @rename-section="(sid, name) => emit('rename-section', sid, name)"
      @toggle-collapse="sid => emit('toggle-collapse', sid)"
      @delete-section="sid => emit('delete-section', sid)"
    />
  </section>
</template>

<style scoped>
.section-block { contain: layout; }
.add-slot:focus { outline: none; }

/* --- Edit-mode framing for tiles & widgets --- */
.edit-frame {
  /* Always-visible dashed border so the whole rectangle reads as editable */
  border: 1px dashed rgb(148 163 184 / 0.5);
  transition: background-color 120ms ease, border-color 120ms ease, border-style 120ms ease;
}
.edit-frame:hover {
  background-color: rgb(20 184 166 / 0.06);
  border-style: solid;
  border-color: rgb(20 184 166 / 0.7);
  border-width: 2px;
}

/* --- Resize affordance: corner bracket that hugs the tile's rounded-xl corner --- */
:deep(.vgl-item) {
  --vgl-resizer-size: 28px;
  --vgl-resizer-border-width: 0px; /* hide library default; we paint our own */
}
:deep(.vgl-item__resizer) {
  /* Bigger hit area, flush bottom-right */
  width: 28px;
  height: 28px;
  right: 0;
  bottom: 0;
  z-index: 2;
}
:deep(.vgl-item__resizer)::before {
  /* Arc that sits ON the tile's border, matching its rounded-xl (12px) corner */
  content: '';
  position: absolute;
  inset: auto 0 0 auto;
  width: 22px;
  height: 22px;
  border: 0;
  border-right: 3px solid rgb(20 184 166 / 0.75);
  border-bottom: 3px solid rgb(20 184 166 / 0.75);
  border-bottom-right-radius: 12px;
  transition: border-color 120ms ease, border-width 120ms ease;
}
:deep(.vgl-item:hover .vgl-item__resizer)::before {
  border-right-color: rgb(20 184 166);
  border-bottom-color: rgb(20 184 166);
  border-right-width: 4px;
  border-bottom-width: 4px;
}
:deep(.vgl-item__resizer--rtl)::before {
  inset: auto auto 0 0;
  border-right: 0;
  border-left: 3px solid rgb(20 184 166 / 0.75);
  border-bottom-right-radius: 0;
  border-bottom-left-radius: 12px;
}

</style>
