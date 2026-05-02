import { onBeforeUnmount, onMounted, ref } from 'vue'

const now = ref(new Date())
let subscribers = 0
let timeoutId: ReturnType<typeof setTimeout> | null = null

function tick() {
  now.value = new Date()
  schedule()
}

function schedule() {
  // Align to the next wall-clock second so all subscribers update together,
  // exactly when the second rolls over.
  const delay = 1000 - (Date.now() % 1000)
  timeoutId = setTimeout(tick, delay)
}

/**
 * Shared, ref-counted "current time" source. Every consumer receives the
 * same `Date` ref, so multiple clocks always render the same second.
 */
export function useNow() {
  onMounted(() => {
    subscribers++
    if (subscribers === 1) schedule()
  })
  onBeforeUnmount(() => {
    subscribers--
    if (subscribers <= 0) {
      subscribers = 0
      if (timeoutId !== null) {
        clearTimeout(timeoutId)
        timeoutId = null
      }
    }
  })
  return now
}
