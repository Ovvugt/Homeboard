<script setup lang="ts">
import { computed, reactive, watch } from 'vue'
import Modal from '@/components/ui/Modal.vue'
import Field from '@/components/ui/Field.vue'
import { widgetsApi } from '@/api/widgets'
import type { WidgetDto } from '@/types/board'
import { WEATHER_CITIES, cityKey, matchCity } from '@/utils/weatherCities'

const props = defineProps<{ open: boolean; widget: WidgetDto | null }>()
const emit = defineEmits<{ close: []; saved: [] }>()

interface FormState {
  // Clock
  format: '12h' | '24h'
  timezone: string
  // shared
  label: string
  // Weather
  latitude: number | null
  longitude: number | null
  // Minecraft
  host: string
  port: number | null
  refreshSeconds: number | null
  showFavicon: boolean
  showMotd: boolean
  // Raw fallback
  raw: string
}

function blank(): FormState {
  return {
    format: '24h',
    timezone: '',
    label: '',
    latitude: null,
    longitude: null,
    host: '',
    port: 25565,
    refreshSeconds: 60,
    showFavicon: true,
    showMotd: true,
    raw: '{}',
  }
}

const form = reactive<FormState>(blank())
const submitting = reactive({ value: false, error: '' })

interface TzOption { label: string; value: string; sortKey: number }

// Curated DST-aware IANA zones, one representative per common offset.
const TZ_REGIONS: { value: string; city: string }[] = [
  { value: 'Pacific/Midway', city: 'Midway' },
  { value: 'Pacific/Honolulu', city: 'Honolulu' },
  { value: 'America/Anchorage', city: 'Anchorage' },
  { value: 'America/Los_Angeles', city: 'Los Angeles' },
  { value: 'America/Denver', city: 'Denver' },
  { value: 'America/Chicago', city: 'Chicago' },
  { value: 'America/New_York', city: 'New York' },
  { value: 'America/Halifax', city: 'Halifax' },
  { value: 'America/Sao_Paulo', city: 'São Paulo' },
  { value: 'Atlantic/Azores', city: 'Azores' },
  { value: 'UTC', city: 'UTC' },
  { value: 'Europe/London', city: 'London' },
  { value: 'Europe/Paris', city: 'Paris / Amsterdam / Berlin' },
  { value: 'Europe/Athens', city: 'Athens / Helsinki' },
  { value: 'Europe/Moscow', city: 'Moscow' },
  { value: 'Asia/Dubai', city: 'Dubai' },
  { value: 'Asia/Karachi', city: 'Karachi' },
  { value: 'Asia/Kolkata', city: 'Kolkata / Mumbai' },
  { value: 'Asia/Dhaka', city: 'Dhaka' },
  { value: 'Asia/Bangkok', city: 'Bangkok' },
  { value: 'Asia/Shanghai', city: 'Shanghai / Singapore' },
  { value: 'Asia/Tokyo', city: 'Tokyo / Seoul' },
  { value: 'Australia/Sydney', city: 'Sydney' },
  { value: 'Pacific/Noumea', city: 'Nouméa' },
  { value: 'Pacific/Auckland', city: 'Auckland' },
]

function offsetMinutes(timeZone: string): number {
  // Compare wall-clock time in the zone vs UTC for a single instant.
  const now = new Date()
  const parts = new Intl.DateTimeFormat('en-US', {
    timeZone,
    hour12: false,
    year: 'numeric', month: '2-digit', day: '2-digit',
    hour: '2-digit', minute: '2-digit', second: '2-digit',
  }).formatToParts(now)
  const get = (t: string) => Number(parts.find(p => p.type === t)?.value)
  const asUtc = Date.UTC(get('year'), get('month') - 1, get('day'), get('hour') % 24, get('minute'), get('second'))
  return Math.round((asUtc - now.getTime()) / 60000)
}

function formatOffset(min: number): string {
  const sign = min >= 0 ? '+' : '−' // proper minus sign for visual alignment
  const abs = Math.abs(min)
  const h = String(Math.floor(abs / 60)).padStart(2, '0')
  const m = String(abs % 60).padStart(2, '0')
  return `UTC ${sign}${h}:${m}`
}

const timezones = computed<TzOption[]>(() => {
  const opts = TZ_REGIONS.map(r => {
    const min = offsetMinutes(r.value)
    return {
      value: r.value,
      sortKey: min,
      label: `${formatOffset(min)} · ${r.city}`,
    }
  })
  opts.sort((a, b) => a.sortKey - b.sortKey || a.label.localeCompare(b.label))
  return opts
})

const browserTimezone = computed(() => {
  try { return Intl.DateTimeFormat().resolvedOptions().timeZone || 'UTC' } catch { return 'UTC' }
})

const weatherCityKey = computed<string>({
  get() {
    const match = matchCity(form.latitude, form.longitude)
    return match ? cityKey(match) : ''
  },
  set(key) {
    if (!key) return
    const city = WEATHER_CITIES.find(c => cityKey(c) === key)
    if (!city) return
    form.latitude = city.lat
    form.longitude = city.lon
  },
})

watch(() => [props.open, props.widget?.id, props.widget?.configJson], ([open]) => {
  if (!open || !props.widget) return
  Object.assign(form, blank())
  let parsed: Record<string, unknown> = {}
  try { parsed = JSON.parse(props.widget.configJson) } catch { parsed = {} }
  form.raw = props.widget.configJson || '{}'
  if (typeof parsed.label === 'string') form.label = parsed.label
  if (props.widget.type === 'Clock') {
    if (parsed.format === '12h' || parsed.format === '24h') form.format = parsed.format
    if (typeof parsed.timezone === 'string') form.timezone = parsed.timezone
  } else if (props.widget.type === 'Weather') {
    if (typeof parsed.latitude === 'number') form.latitude = parsed.latitude
    if (typeof parsed.longitude === 'number') form.longitude = parsed.longitude
  } else if (props.widget.type === 'Minecraft') {
    if (typeof parsed.host === 'string') form.host = parsed.host
    if (typeof parsed.port === 'number') form.port = parsed.port
    if (typeof parsed.refreshSeconds === 'number') form.refreshSeconds = parsed.refreshSeconds
    if (typeof parsed.showFavicon === 'boolean') form.showFavicon = parsed.showFavicon
    if (typeof parsed.showMotd === 'boolean') form.showMotd = parsed.showMotd
  }
  submitting.error = ''
}, { immediate: true })

function buildConfig(): string {
  if (!props.widget) return '{}'
  if (props.widget.type === 'Clock') {
    const out: Record<string, unknown> = { format: form.format }
    if (form.timezone) out.timezone = form.timezone
    if (form.label) out.label = form.label
    return JSON.stringify(out)
  }
  if (props.widget.type === 'Weather') {
    const out: Record<string, unknown> = {}
    if (form.latitude != null) out.latitude = Number(form.latitude)
    if (form.longitude != null) out.longitude = Number(form.longitude)
    if (form.label) out.label = form.label
    return JSON.stringify(out)
  }
  if (props.widget.type === 'Minecraft') {
    const out: Record<string, unknown> = { host: form.host.trim() }
    if (form.port != null) out.port = Number(form.port)
    if (form.refreshSeconds != null) out.refreshSeconds = Number(form.refreshSeconds)
    if (form.label) out.label = form.label
    if (!form.showFavicon) out.showFavicon = false
    if (!form.showMotd) out.showMotd = false
    return JSON.stringify(out)
  }
  // Unknown type — let the user edit raw JSON.
  return form.raw
}

const rawValid = computed(() => {
  if (!props.widget) return true
  if (props.widget.type === 'Clock' || props.widget.type === 'Weather' || props.widget.type === 'Minecraft') return true
  try { JSON.parse(form.raw); return true } catch { return false }
})

async function save() {
  if (!props.widget) return
  if (!rawValid.value) {
    submitting.error = 'Config JSON is not valid.'
    return
  }
  submitting.value = true
  submitting.error = ''
  try {
    await widgetsApi.update(props.widget.id, buildConfig())
    emit('saved')
    emit('close')
  } catch (e) {
    submitting.error = e instanceof Error ? e.message : String(e)
  } finally {
    submitting.value = false
  }
}

async function remove() {
  if (!props.widget) return
  if (!confirm(`Delete this ${props.widget.type} widget?`)) return
  submitting.value = true
  try {
    await widgetsApi.delete(props.widget.id)
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
  <Modal :open="open" :title="widget ? `Edit ${widget.type} widget` : 'Edit widget'" @close="emit('close')">
    <form v-if="widget" class="space-y-4" @submit.prevent="save" @keydown.ctrl.enter.prevent="save" @keydown.meta.enter.prevent="save">
      <template v-if="widget.type === 'Clock'">
        <Field label="Format">
          <select v-model="form.format" :class="inputClass">
            <option value="24h">24-hour</option>
            <option value="12h">12-hour</option>
          </select>
        </Field>
        <Field label="Timezone" :hint="`Blank uses browser local (${browserTimezone}). Offsets shown are current; zones observe DST.`">
          <select v-model="form.timezone" :class="inputClass">
            <option value="">Browser local</option>
            <option v-for="tz in timezones" :key="tz.value" :value="tz.value">{{ tz.label }}</option>
          </select>
        </Field>
        <Field label="Label (optional)">
          <input v-model="form.label" :class="inputClass" />
        </Field>
      </template>

      <template v-else-if="widget.type === 'Weather'">
        <Field label="City" hint="Pick a preset or enter custom coordinates below.">
          <select v-model="weatherCityKey" :class="inputClass">
            <option value="">Custom coordinates</option>
            <option v-for="c in WEATHER_CITIES" :key="cityKey(c)" :value="cityKey(c)">
              {{ c.name }} · {{ c.country }}
            </option>
          </select>
        </Field>
        <div class="grid grid-cols-2 gap-3">
          <Field label="Latitude">
            <input v-model.number="form.latitude" type="number" step="0.0001" :class="inputClass" />
          </Field>
          <Field label="Longitude">
            <input v-model.number="form.longitude" type="number" step="0.0001" :class="inputClass" />
          </Field>
        </div>
        <Field label="Label (optional)" hint="Defaults to the selected city.">
          <input v-model="form.label" :class="inputClass" placeholder="Amsterdam" />
        </Field>
      </template>

      <template v-else-if="widget.type === 'Minecraft'">
        <Field label="Host" hint="Hostname or IP. Default port 25565 if omitted.">
          <input v-model="form.host" :class="inputClass" required placeholder="mc.example.com" />
        </Field>
        <div class="grid grid-cols-2 gap-3">
          <Field label="Port">
            <input v-model.number="form.port" type="number" min="1" max="65535" :class="inputClass" />
          </Field>
          <Field label="Refresh (seconds)" hint="Minimum 15s.">
            <input v-model.number="form.refreshSeconds" type="number" min="15" max="3600" :class="inputClass" />
          </Field>
        </div>
        <Field label="Label (optional)" hint="Defaults to host:port.">
          <input v-model="form.label" :class="inputClass" />
        </Field>
        <div class="flex gap-4 text-sm text-gray-700 dark:text-gray-300">
          <label class="inline-flex items-center gap-2">
            <input v-model="form.showFavicon" type="checkbox" class="rounded border-gray-300" />
            Show favicon
          </label>
          <label class="inline-flex items-center gap-2">
            <input v-model="form.showMotd" type="checkbox" class="rounded border-gray-300" />
            Show MOTD
          </label>
        </div>
      </template>

      <template v-else>
        <Field label="Config JSON">
          <textarea v-model="form.raw" rows="6" class="font-mono text-xs" :class="inputClass" />
          <span v-if="!rawValid" class="block text-xs text-red-600 mt-1">Invalid JSON</span>
        </Field>
      </template>

      <div v-if="submitting.error" class="text-sm text-red-600">{{ submitting.error }}</div>
    </form>
    <template #footer="{ close }">
      <button
        v-if="widget"
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
        :disabled="submitting.value || !rawValid"
        @click="save"
      >
        {{ submitting.value ? 'Saving…' : 'Save' }}
      </button>
    </template>
  </Modal>
</template>
