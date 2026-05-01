import { ref, shallowRef } from 'vue'

export function useApi<T, Args extends unknown[] = []>(
  fetcher: (...args: Args) => Promise<T>
) {
  const data = shallowRef<T | null>(null)
  const loading = ref(false)
  const error = ref<Error | null>(null)

  async function execute(...args: Args): Promise<T | null> {
    loading.value = data.value === null
    error.value = null
    try {
      const result = await fetcher(...args)
      data.value = result
      return result
    } catch (e) {
      error.value = e instanceof Error ? e : new Error(String(e))
      return null
    } finally {
      loading.value = false
    }
  }

  function reset() {
    data.value = null
    error.value = null
    loading.value = false
  }

  return { data, loading, error, execute, reset }
}
