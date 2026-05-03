<script setup lang="ts">
import Modal from '@/components/ui/Modal.vue'
import { widgetsApi } from '@/api/widgets'
import type { TileDto, WidgetDto, WidgetType } from '@/types/board'
import { findFreeSpot, type GridRect } from '@/utils/placement'

const props = defineProps<{
  open: boolean
  boardId: string
  existingTiles?: TileDto[]
  existingWidgets?: WidgetDto[]
  defaultSectionId?: string | null
  gridColumns?: number
}>()
const emit = defineEmits<{ close: []; saved: [] }>()

async function pick(type: WidgetType) {
  let config: string
  switch (type) {
    case 'Clock':
      config = '{"format":"24h"}'
      break
    case 'Weather':
      config = '{"latitude":52.37,"longitude":4.9,"label":"Amsterdam","units":"metric"}'
      break
    case 'Minecraft':
      config = '{"host":"mc.example.com","port":25565,"refreshSeconds":60}'
      break
  }
  const w = 3
  const h = 2
  const columns = props.gridColumns ?? 12
  const sectionId = props.defaultSectionId ?? null
  const inSection = (item: { sectionId: string | null }) => (item.sectionId ?? null) === sectionId
  const occupants: GridRect[] = [
    ...(props.existingTiles ?? []).filter(inSection),
    ...(props.existingWidgets ?? []).filter(inSection),
  ]
  const { x, y } = findFreeSpot(occupants, columns, w, h)
  await widgetsApi.create({
    boardId: props.boardId,
    sectionId,
    type,
    gridX: x,
    gridY: y,
    gridW: w,
    gridH: h,
    configJson: config,
  })
  emit('saved')
  emit('close')
}
</script>

<template>
  <Modal :open="open" title="Add widget" @close="emit('close')">
    <div class="grid grid-cols-2 gap-3">
      <button
        type="button"
        class="rounded-lg border border-gray-200 dark:border-gray-700 p-4 text-left hover:border-primary-400 hover:bg-primary-50 dark:hover:bg-primary-950/20 transition"
        @click="pick('Clock')"
      >
        <div class="font-medium text-gray-900 dark:text-gray-100">Clock</div>
        <div class="text-sm text-gray-500">Local time, 12h or 24h.</div>
      </button>
      <button
        type="button"
        class="rounded-lg border border-gray-200 dark:border-gray-700 p-4 text-left hover:border-primary-400 hover:bg-primary-50 dark:hover:bg-primary-950/20 transition"
        @click="pick('Weather')"
      >
        <div class="font-medium text-gray-900 dark:text-gray-100">Weather</div>
        <div class="text-sm text-gray-500">Current conditions via Open-Meteo.</div>
      </button>
      <button
        type="button"
        class="rounded-lg border border-gray-200 dark:border-gray-700 p-4 text-left hover:border-primary-400 hover:bg-primary-50 dark:hover:bg-primary-950/20 transition"
        @click="pick('Minecraft')"
      >
        <div class="font-medium text-gray-900 dark:text-gray-100">Minecraft</div>
        <div class="text-sm text-gray-500">Live server status & player count via SLP.</div>
      </button>
    </div>
    <template #footer="{ close }">
      <button
        type="button"
        class="px-3 py-1.5 rounded-md text-sm text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800"
        @click="close"
      >Cancel</button>
    </template>
  </Modal>
</template>
