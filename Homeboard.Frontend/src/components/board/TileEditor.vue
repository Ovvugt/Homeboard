<script setup lang="ts">
import { reactive, watch } from 'vue'
import Modal from '@/components/ui/Modal.vue'
import Field from '@/components/ui/Field.vue'
import { tilesApi } from '@/api/tiles'
import type { CreateTileDto, TileDto, TileIconKind, TileStatusType, UpdateTileDto, WidgetDto } from '@/types/board'
import { findFreeSpot, type GridRect } from '@/utils/placement'

const props = defineProps<{
  open: boolean
  boardId: string
  tile: TileDto | null
  existingTiles?: TileDto[]
  existingWidgets?: WidgetDto[]
  gridColumns?: number
}>()

const emit = defineEmits<{
  close: []
  saved: []
}>()

interface FormState {
  name: string
  url: string
  iconUrl: string
  iconKind: TileIconKind
  description: string
  color: string
  statusType: TileStatusType
  statusTarget: string
  statusInterval: number
  statusTimeout: number
}

function blank(): FormState {
  return {
    name: '',
    url: '',
    iconUrl: '',
    iconKind: 'Url',
    description: '',
    color: '',
    // Default to HTTP GET against the tile URL — most users want the dot.
    // Advanced users can switch the type or set a separate target.
    statusType: 'HttpGet',
    statusTarget: '',
    statusInterval: 60,
    statusTimeout: 5000,
  }
}

const form = reactive<FormState>(blank())
const submitting = reactive({ value: false, error: '' })

watch(() => [props.open, props.tile], ([open]) => {
  if (!open) return
  Object.assign(form, blank())
  if (props.tile) {
    form.name = props.tile.name
    form.url = props.tile.url
    form.iconUrl = props.tile.iconUrl ?? ''
    form.iconKind = props.tile.iconKind
    form.description = props.tile.description ?? ''
    form.color = props.tile.color ?? ''
    form.statusType = props.tile.statusType
    form.statusTarget = props.tile.statusTarget ?? ''
    form.statusInterval = props.tile.statusInterval
    form.statusTimeout = props.tile.statusTimeout
  }
  submitting.error = ''
}, { immediate: true })

async function save() {
  submitting.value = true
  submitting.error = ''
  // Clamp to backend-enforced bounds — poller cadence floor is 10s.
  form.statusInterval = Math.max(10, Math.min(86400, Number(form.statusInterval) || 60))
  form.statusTimeout = Math.max(100, Math.min(60000, Number(form.statusTimeout) || 5000))
  try {
    if (props.tile) {
      const dto: UpdateTileDto = {
        name: form.name,
        url: form.url,
        iconUrl: form.iconUrl || null,
        iconKind: form.iconKind,
        description: form.description || null,
        color: form.color || null,
        statusType: form.statusType,
        statusTarget: form.statusTarget || null,
        statusInterval: form.statusInterval,
        statusTimeout: form.statusTimeout,
        statusExpected: null,
      }
      await tilesApi.update(props.tile.id, dto)
    } else {
      const w = 3
      const h = 2
      const columns = props.gridColumns ?? 12
      const occupants: GridRect[] = [
        ...(props.existingTiles ?? []),
        ...(props.existingWidgets ?? []),
      ]
      const { x, y } = findFreeSpot(occupants, columns, w, h)
      const dto: CreateTileDto = {
        boardId: props.boardId,
        name: form.name,
        url: form.url,
        iconUrl: form.iconUrl || null,
        iconKind: form.iconKind,
        description: form.description || null,
        color: form.color || null,
        gridX: x,
        gridY: y,
        gridW: w,
        gridH: h,
        statusType: form.statusType,
        statusTarget: form.statusTarget || null,
        statusInterval: form.statusInterval,
        statusTimeout: form.statusTimeout,
        statusExpected: null,
      }
      await tilesApi.create(dto)
    }
    emit('saved')
    emit('close')
  } catch (e) {
    submitting.error = e instanceof Error ? e.message : String(e)
  } finally {
    submitting.value = false
  }
}

async function remove() {
  if (!props.tile) return
  if (!confirm(`Delete tile "${props.tile.name}"?`)) return
  submitting.value = true
  try {
    await tilesApi.delete(props.tile.id)
    emit('saved')
    emit('close')
  } catch (e) {
    submitting.error = e instanceof Error ? e.message : String(e)
  } finally {
    submitting.value = false
  }
}

const inputClass = 'mt-1 block w-full rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-primary-500'
</script>

<template>
  <Modal :open="open" :title="tile ? 'Edit tile' : 'Add tile'" @close="emit('close')">
    <form class="space-y-4" @submit.prevent="save">
      <Field label="Name">
        <input v-model="form.name" :class="inputClass" required maxlength="120" />
      </Field>
      <Field label="URL" hint="Where this tile links to.">
        <input v-model="form.url" :class="inputClass" required placeholder="https://..." />
      </Field>
      <div class="grid grid-cols-2 gap-3">
        <Field label="Icon URL (optional)">
          <input v-model="form.iconUrl" :class="inputClass" placeholder="https://.../icon.png" />
        </Field>
        <Field label="Color (optional)">
          <input v-model="form.color" :class="inputClass" placeholder="#0d9488" />
        </Field>
      </div>
      <Field label="Description (optional)">
        <input v-model="form.description" :class="inputClass" maxlength="200" />
      </Field>

      <fieldset class="border border-gray-200 dark:border-gray-700 rounded-md p-3 space-y-3">
        <legend class="px-2 text-sm font-medium text-gray-700 dark:text-gray-300">Status check</legend>
        <Field label="Type">
          <select v-model="form.statusType" :class="inputClass">
            <option value="None">None</option>
            <option value="HttpHead">HTTP HEAD</option>
            <option value="HttpGet">HTTP GET</option>
            <option value="Tcp">TCP</option>
          </select>
        </Field>
        <template v-if="form.statusType !== 'None'">
          <Field label="Target (optional)" hint="Leave blank to check the tile's URL. HTTP: full URL. TCP: host:port.">
            <input v-model="form.statusTarget" :class="inputClass" />
          </Field>
          <div class="grid grid-cols-2 gap-3">
            <Field label="Interval (s)" hint="Minimum 10s — the poller wakes every 10s.">
              <input v-model.number="form.statusInterval" type="number" min="10" max="86400" :class="inputClass" />
            </Field>
            <Field label="Timeout (ms)">
              <input v-model.number="form.statusTimeout" type="number" min="100" max="60000" :class="inputClass" />
            </Field>
          </div>
        </template>
      </fieldset>

      <div v-if="submitting.error" class="text-sm text-red-600">{{ submitting.error }}</div>
    </form>
    <template #footer="{ close }">
      <button
        v-if="tile"
        type="button"
        class="mr-auto px-3 py-1.5 rounded-md text-sm text-red-600 hover:bg-red-50 dark:hover:bg-red-950/30"
        :disabled="submitting.value"
        @click="remove"
      >
        Delete
      </button>
      <button
        type="button"
        class="px-3 py-1.5 rounded-md text-sm text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800"
        :disabled="submitting.value"
        @click="close"
      >
        Cancel
      </button>
      <button
        type="button"
        class="px-3 py-1.5 rounded-md text-sm bg-primary-600 text-white hover:bg-primary-700 disabled:opacity-60"
        :disabled="submitting.value || !form.name || !form.url"
        @click="save"
      >
        {{ submitting.value ? 'Saving…' : 'Save' }}
      </button>
    </template>
  </Modal>
</template>
