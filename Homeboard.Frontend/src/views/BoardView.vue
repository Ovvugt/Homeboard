<script setup lang="ts">
import { computed, onMounted, onBeforeUnmount, ref, watch } from 'vue'
import { PlusIcon, FolderPlusIcon, Squares2X2Icon } from '@heroicons/vue/24/outline'
import { useBoardsStore } from '@/stores/boards'
import { useEditModeStore } from '@/stores/editMode'
import { useStatusStore } from '@/stores/status'
import { tilesApi } from '@/api/tiles'
import { widgetsApi } from '@/api/widgets'
import { sectionsApi } from '@/api/sections'
import BoardSections from '@/components/board/BoardSections.vue'
import TileEditor from '@/components/board/TileEditor.vue'
import WidgetPicker from '@/components/board/WidgetPicker.vue'
import WidgetEditor from '@/components/board/WidgetEditor.vue'

const props = defineProps<{ slug: string }>()
const boards = useBoardsStore()
const edit = useEditModeStore()
const status = useStatusStore()

async function loadAndPoll(slug: string) {
  await boards.loadBySlug(slug)
  if (boards.current) status.start(boards.current.id)
}
onMounted(() => loadAndPoll(props.slug))
watch(() => props.slug, slug => loadAndPoll(slug))
onBeforeUnmount(() => status.stop())

const tileEditorOpen = ref(false)
const editingTileId = ref<string | null>(null)
const widgetPickerOpen = ref(false)
const widgetEditorOpen = ref(false)
const editingWidgetId = ref<string | null>(null)
const defaultSectionId = ref<string | null>(null)

const editingTile = computed(() => {
  if (!editingTileId.value || !boards.current) return null
  return boards.current.tiles.find(t => t.id === editingTileId.value) ?? null
})

const editingWidget = computed(() => {
  if (!editingWidgetId.value || !boards.current) return null
  return boards.current.widgets.find(w => w.id === editingWidgetId.value) ?? null
})

function openCreateTile(sectionId: string | null = null) {
  editingTileId.value = null
  defaultSectionId.value = sectionId
  tileEditorOpen.value = true
}

function openWidgetPicker(sectionId: string | null = null) {
  defaultSectionId.value = sectionId
  widgetPickerOpen.value = true
}

function onEditTile(id: string) {
  editingTileId.value = id
  tileEditorOpen.value = true
}

function onEditWidget(id: string) {
  editingWidgetId.value = id
  widgetEditorOpen.value = true
}

async function onDeleteTile(id: string) {
  try {
    await tilesApi.delete(id)
  } finally {
    await refresh()
  }
}

async function onDeleteWidget(id: string) {
  try {
    await widgetsApi.delete(id)
  } finally {
    await refresh()
  }
}

async function refresh() {
  await boards.loadBySlug(props.slug)
  if (boards.current) status.refresh(boards.current.id)
}

async function addSection() {
  if (!boards.current) return
  const name = prompt('Section name')
  if (name === null) return
  const root = boards.current.sections.find(s => s.parentId === null)
  if (!root) return
  try {
    await sectionsApi.create({
      boardId: boards.current.id,
      parentId: root.id,
      name: name.trim() || null,
    })
  } finally {
    await refresh()
  }
}
</script>

<template>
  <div>
    <div v-if="boards.loading && !boards.current" class="p-6 text-gray-500">Loading…</div>
    <div v-else-if="boards.error" class="p-6 text-red-600">Failed to load board: {{ boards.error.message }}</div>
    <div v-else-if="!boards.current" class="p-6 text-gray-500">Board not found.</div>
    <template v-else>
      <div class="px-6 pt-6 flex items-baseline justify-between">
        <h1 class="font-display text-3xl text-gray-900 dark:text-gray-100">{{ boards.current.name }}</h1>
        <div v-if="edit.editing" class="flex items-center gap-2">
          <button
            type="button"
            class="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-md text-sm bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700"
            @click="addSection"
          >
            <FolderPlusIcon class="w-4 h-4" />
            Add section
          </button>
          <button
            type="button"
            class="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-md text-sm bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700"
            @click="openWidgetPicker(null)"
          >
            <Squares2X2Icon class="w-4 h-4" />
            Add widget
          </button>
          <button
            type="button"
            class="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-md text-sm bg-primary-600 text-white hover:bg-primary-700"
            @click="openCreateTile(null)"
          >
            <PlusIcon class="w-4 h-4" />
            Add tile
          </button>
        </div>
      </div>
      <div
        v-if="boards.current.tiles.length === 0 && boards.current.widgets.length === 0 && !edit.editing"
        class="px-6 mt-12 text-center"
      >
        <div class="font-display text-2xl text-gray-700 dark:text-gray-200">Nothing here yet</div>
        <p class="text-sm text-gray-500 mt-2 mb-6">Switch to edit mode and add your first tile or widget.</p>
        <button
          type="button"
          class="inline-flex items-center gap-1.5 px-4 py-2 rounded-md text-sm bg-primary-600 text-white hover:bg-primary-700"
          @click="edit.editing = true; openCreateTile(null)"
        >
          <PlusIcon class="w-4 h-4" />
          Add a tile
        </button>
      </div>
      <BoardSections
        v-else
        :board="boards.current"
        :editable="edit.editing"
        @edit-tile="onEditTile"
        @edit-widget="onEditWidget"
        @delete-tile="onDeleteTile"
        @delete-widget="onDeleteWidget"
        @add-tile="openCreateTile"
        @add-widget="openWidgetPicker"
        @changed="refresh"
      />
      <TileEditor
        :open="tileEditorOpen"
        :board-id="boards.current.id"
        :tile="editingTile"
        :existing-tiles="boards.current.tiles"
        :existing-widgets="boards.current.widgets"
        :sections="boards.current.sections"
        :default-section-id="defaultSectionId"
        :grid-columns="boards.current.gridColumns"
        @close="tileEditorOpen = false"
        @saved="refresh"
      />
      <WidgetPicker
        :open="widgetPickerOpen"
        :board-id="boards.current.id"
        :existing-tiles="boards.current.tiles"
        :existing-widgets="boards.current.widgets"
        :default-section-id="defaultSectionId"
        :grid-columns="boards.current.gridColumns"
        @close="widgetPickerOpen = false"
        @saved="refresh"
      />
      <WidgetEditor
        :open="widgetEditorOpen"
        :widget="editingWidget"
        @close="widgetEditorOpen = false"
        @saved="refresh"
      />
    </template>
  </div>
</template>
