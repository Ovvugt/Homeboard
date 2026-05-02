<script setup lang="ts">
import { ref } from 'vue'
import { useThemeStore, type ThemePreference } from '@/stores/theme'
import { useBoardsStore } from '@/stores/boards'
import Modal from '@/components/ui/Modal.vue'
import {
  buildExport,
  downloadExport,
  importBoards,
  parseExport,
  type BoardExport,
  type ImportResult,
} from '@/utils/boardExport'

const theme = useThemeStore()
const boards = useBoardsStore()

const dbPath = (() => {
  const ua = navigator.userAgent
  if (/Windows/i.test(ua)) return '%LOCALAPPDATA%\\Homeboard\\homeboard.db'
  if (/Mac OS X|Macintosh/i.test(ua)) return '~/Library/Application Support/Homeboard/homeboard.db'
  return '~/.local/share/Homeboard/homeboard.db'
})()

const themeOptions: { value: ThemePreference; label: string }[] = [
  { value: 'light', label: 'Light' },
  { value: 'system', label: 'System' },
  { value: 'dark', label: 'Dark' },
]

const exporting = ref(false)
const importing = ref(false)
const message = ref<{ kind: 'info' | 'error' | 'success'; text: string } | null>(null)
const lastImport = ref<ImportResult | null>(null)
const fileInput = ref<HTMLInputElement | null>(null)

const conflictModalOpen = ref(false)
const pendingImport = ref<{ data: BoardExport; conflicts: string[] } | null>(null)

async function onExport() {
  message.value = null
  exporting.value = true
  try {
    const data = await buildExport()
    const stamp = new Date().toISOString().replace(/[:.]/g, '-').slice(0, 19)
    downloadExport(data, `homeboard-export-${stamp}.json`)
    message.value = { kind: 'success', text: `Exported ${data.boards.length} board(s).` }
  } catch (e) {
    message.value = { kind: 'error', text: e instanceof Error ? e.message : String(e) }
  } finally {
    exporting.value = false
  }
}

function pickFile() {
  fileInput.value?.click()
}

async function onFileChosen(ev: Event) {
  const input = ev.target as HTMLInputElement
  const file = input.files?.[0]
  input.value = ''
  if (!file) return

  message.value = null
  lastImport.value = null
  try {
    const text = await file.text()
    const data = parseExport(text)
    const conflicts = data.boards.map(b => b.slug).filter(s => boards.list.some(b => b.slug === s))
    if (conflicts.length) {
      pendingImport.value = { data, conflicts }
      conflictModalOpen.value = true
      return
    }
    await runImport(data, false)
  } catch (e) {
    message.value = { kind: 'error', text: e instanceof Error ? e.message : String(e) }
  }
}

async function runImport(data: BoardExport, overwrite: boolean) {
  importing.value = true
  try {
    const result = await importBoards(data, { overwrite })
    lastImport.value = result
    await boards.loadList()

    const parts: string[] = []
    if (result.created.length) parts.push(`${result.created.length} created`)
    if (result.replaced.length) parts.push(`${result.replaced.length} replaced`)
    if (result.skipped.length) parts.push(`${result.skipped.length} skipped`)
    if (result.failed.length) parts.push(`${result.failed.length} failed`)
    const kind = result.failed.length
      ? 'error'
      : (result.created.length || result.replaced.length) ? 'success' : 'info'
    message.value = {
      kind,
      text: parts.length ? `Import complete: ${parts.join(', ')}.` : 'Nothing to import.',
    }
  } catch (e) {
    message.value = { kind: 'error', text: e instanceof Error ? e.message : String(e) }
  } finally {
    importing.value = false
  }
}

function closeConflictModal() {
  conflictModalOpen.value = false
  pendingImport.value = null
}

async function confirmOverwrite() {
  const pending = pendingImport.value
  conflictModalOpen.value = false
  pendingImport.value = null
  if (pending) await runImport(pending.data, true)
}

async function confirmSkip() {
  const pending = pendingImport.value
  conflictModalOpen.value = false
  pendingImport.value = null
  if (pending) await runImport(pending.data, false)
}
</script>

<template>
  <div class="p-6 max-w-2xl">
    <h1 class="font-display text-3xl mb-6 text-gray-900 dark:text-gray-100">Settings</h1>

    <section class="mb-8">
      <h2 class="text-sm font-semibold uppercase tracking-wider text-gray-500 mb-3">Appearance</h2>
      <div class="inline-flex rounded-md border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 p-1">
        <button
          v-for="opt in themeOptions"
          :key="opt.value"
          type="button"
          class="px-4 py-1.5 text-sm rounded transition"
          :class="theme.preference === opt.value
            ? 'bg-primary-600 text-white'
            : 'text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700'"
          @click="theme.setPreference(opt.value)"
        >{{ opt.label }}</button>
      </div>
    </section>

    <section class="mb-8">
      <h2 class="text-sm font-semibold uppercase tracking-wider text-gray-500 mb-3">Import / Export</h2>
      <p class="text-sm text-gray-600 dark:text-gray-400 mb-3">
        Save all your boards, tiles, and widgets to a JSON file, or restore from one.
      </p>
      <div class="flex gap-2">
        <button
          type="button"
          class="inline-flex items-center px-3 py-1.5 rounded-md text-sm bg-primary-600 text-white hover:bg-primary-700 disabled:opacity-60"
          :disabled="exporting || importing"
          @click="onExport"
        >
          {{ exporting ? 'Exporting…' : 'Export to JSON' }}
        </button>
        <button
          type="button"
          class="inline-flex items-center px-3 py-1.5 rounded-md text-sm bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700 disabled:opacity-60"
          :disabled="exporting || importing"
          @click="pickFile"
        >
          {{ importing ? 'Importing…' : 'Import from JSON' }}
        </button>
        <input
          ref="fileInput"
          type="file"
          accept="application/json,.json"
          class="hidden"
          @change="onFileChosen"
        />
      </div>
      <div
        v-if="message"
        class="mt-3 text-sm"
        :class="{
          'text-green-700 dark:text-green-400': message.kind === 'success',
          'text-red-600 dark:text-red-400': message.kind === 'error',
          'text-gray-600 dark:text-gray-400': message.kind === 'info',
        }"
      >
        {{ message.text }}
      </div>
      <ul
        v-if="lastImport && (lastImport.replaced.length || lastImport.skipped.length || lastImport.failed.length)"
        class="mt-2 text-xs text-gray-600 dark:text-gray-400 space-y-1"
      >
        <li v-for="r in lastImport.replaced" :key="`repl-${r}`">
          Replaced <span class="font-mono">{{ r }}</span>
        </li>
        <li v-for="s in lastImport.skipped" :key="`skip-${s.slug}`">
          Skipped <span class="font-mono">{{ s.slug }}</span>: {{ s.reason }}
        </li>
        <li v-for="f in lastImport.failed" :key="`fail-${f.slug}`" class="text-red-600 dark:text-red-400">
          Failed <span class="font-mono">{{ f.slug }}</span>: {{ f.reason }}
        </li>
      </ul>
    </section>

    <section class="mb-8">
      <h2 class="text-sm font-semibold uppercase tracking-wider text-gray-500 mb-3">Tips</h2>
      <ul class="text-sm text-gray-700 dark:text-gray-300 space-y-2 list-disc list-inside">
        <li>Click <span class="font-medium">Edit</span> in the header to toggle drag-and-drop layout mode.</li>
        <li>In edit mode, click any tile to edit its target URL or status check.</li>
        <li>Status checks (HTTP/TCP) run every 10 seconds in the background; the dot in the corner of each tile shows online/offline.</li>
        <li>Weather widgets pull from Open-Meteo (no API key needed). Set latitude/longitude in the widget's <span class="font-mono text-xs">configJson</span>.</li>
      </ul>
    </section>

    <section>
      <h2 class="text-sm font-semibold uppercase tracking-wider text-gray-500 mb-3">About</h2>
      <p class="text-sm text-gray-600 dark:text-gray-400">
        Homeboard runs locally &mdash; no auth, no cloud. Data is stored in a SQLite file at
        <span class="font-mono text-xs">{{ dbPath }}</span>.
      </p>
    </section>

    <Modal :open="conflictModalOpen" title="Board already exists" @close="closeConflictModal">
      <p class="text-sm text-gray-700 dark:text-gray-300 mb-3">
        The import file contains
        {{ pendingImport?.conflicts.length === 1 ? 'a board' : 'boards' }}
        with
        {{ pendingImport?.conflicts.length === 1 ? 'a slug' : 'slugs' }}
        that already exist:
      </p>
      <ul class="text-sm text-gray-700 dark:text-gray-300 space-y-1 mb-3 list-disc list-inside">
        <li v-for="slug in pendingImport?.conflicts ?? []" :key="slug">
          <span class="font-mono">{{ slug }}</span>
        </li>
      </ul>
      <p class="text-sm text-gray-600 dark:text-gray-400">
        Overwriting will delete the existing board's tiles and widgets and replace them with the imported version.
      </p>
      <template #footer="{ close }">
        <button
          type="button"
          class="px-3 py-1.5 rounded-md text-sm text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800"
          @click="close"
        >
          Cancel
        </button>
        <button
          type="button"
          class="px-3 py-1.5 rounded-md text-sm bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700"
          @click="confirmSkip"
        >
          Skip conflicts
        </button>
        <button
          type="button"
          class="px-3 py-1.5 rounded-md text-sm bg-red-600 text-white hover:bg-red-700"
          @click="confirmOverwrite"
        >
          Overwrite
        </button>
      </template>
    </Modal>
  </div>
</template>
