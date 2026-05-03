<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { minecraftApi, type MinecraftStatusDto } from '@/api/minecraft'
import type { WidgetDto } from '@/types/board'

const props = defineProps<{ widget: WidgetDto }>()

interface MinecraftConfig {
  host?: string
  port?: number
  label?: string
  refreshSeconds?: number
  showFavicon?: boolean
  showMotd?: boolean
}

const config = computed<MinecraftConfig>(() => {
  try { return JSON.parse(props.widget.configJson) as MinecraftConfig } catch { return {} }
})

const status = ref<MinecraftStatusDto | null>(null)
const error = ref<string | null>(null)
let timer: ReturnType<typeof setInterval> | null = null

async function load() {
  if (!config.value.host) {
    error.value = 'Set host in config'
    return
  }
  try {
    status.value = await minecraftApi.get(config.value.host, config.value.port)
    error.value = null
  } catch (e) {
    error.value = e instanceof Error ? e.message : String(e)
  }
}

function startTimer() {
  if (timer) clearInterval(timer)
  const seconds = Math.max(15, config.value.refreshSeconds ?? 60)
  timer = setInterval(load, seconds * 1000)
}

onMounted(() => {
  load()
  startTimer()
})
onBeforeUnmount(() => { if (timer) clearInterval(timer) })
watch(() => props.widget.configJson, () => {
  load()
  startTimer()
})

const showFavicon = computed(() => config.value.showFavicon !== false)
const showMotd = computed(() => config.value.showMotd !== false)

const heading = computed(() => {
  if (config.value.label) return config.value.label
  if (!status.value) return 'Minecraft'
  return status.value.port === 25565
    ? status.value.host
    : `${status.value.host}:${status.value.port}`
})
</script>

<template>
  <div class="h-full w-full rounded-xl bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 shadow-sm p-4 flex flex-col select-none overflow-hidden">
    <div class="flex items-center gap-2 min-w-0">
      <img
        v-if="showFavicon && status?.faviconDataUri"
        :src="status.faviconDataUri"
        alt=""
        class="h-8 w-8 rounded-md flex-shrink-0 [image-rendering:pixelated]"
      />
      <div class="min-w-0 flex-1">
        <div class="text-xs uppercase tracking-wider text-gray-500 truncate">{{ heading }}</div>
        <div class="flex items-center gap-1.5 mt-0.5">
          <span
            class="inline-block h-2 w-2 rounded-full flex-shrink-0"
            :class="status?.online ? 'bg-emerald-500' : 'bg-red-500'"
          />
          <span class="text-xs text-gray-600 dark:text-gray-300">
            {{ status?.online ? 'Online' : (error ? 'Error' : 'Offline') }}
          </span>
        </div>
      </div>
    </div>

    <div v-if="error" class="text-xs text-red-600 mt-2 break-words">{{ error }}</div>

    <template v-else-if="status?.online">
      <div class="font-display text-3xl md:text-4xl text-gray-900 dark:text-gray-100 tabular-nums mt-2 leading-none">
        {{ status.playersOnline ?? '—' }}<span
          v-if="status.playersMax != null"
          class="text-lg text-gray-500"
        >/{{ status.playersMax }}</span>
      </div>
      <div class="text-xs text-gray-500 mt-0.5">
        player<span v-if="status.playersOnline !== 1">s</span> online
      </div>
      <div
        v-if="status.playerSample && status.playerSample.length"
        class="text-xs text-gray-600 dark:text-gray-300 mt-1 truncate"
        :title="status.playerSample.join(', ')"
      >
        {{ status.playerSample.join(', ') }}
      </div>
      <div
        v-if="showMotd && status.motd"
        class="text-xs text-gray-500 mt-1 line-clamp-2 whitespace-pre-line"
      >
        {{ status.motd }}
      </div>
      <div class="text-[10px] text-gray-400 mt-auto pt-1 flex gap-2">
        <span v-if="status.versionName">{{ status.versionName }}</span>
        <span v-if="status.latencyMs != null">· {{ status.latencyMs }} ms</span>
      </div>
    </template>

    <div v-else-if="!status" class="text-sm text-gray-500 mt-2">Loading…</div>

    <div v-else class="text-xs text-gray-500 mt-2 break-words">
      {{ status.error || 'Server unreachable' }}
    </div>
  </div>
</template>
