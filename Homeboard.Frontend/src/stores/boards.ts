import { defineStore } from 'pinia'
import { ref } from 'vue'
import { boardsApi } from '@/api/boards'
import type { BoardDetail, BoardSummary } from '@/types/board'

export const useBoardsStore = defineStore('boards', () => {
  const list = ref<BoardSummary[]>([])
  const current = ref<BoardDetail | null>(null)
  const loading = ref(false)
  const error = ref<Error | null>(null)

  async function loadList() {
    list.value = await boardsApi.list()
  }

  async function loadBySlug(slug: string) {
    loading.value = true
    error.value = null
    try {
      current.value = await boardsApi.get(slug)
    } catch (e) {
      error.value = e instanceof Error ? e : new Error(String(e))
    } finally {
      loading.value = false
    }
  }

  return { list, current, loading, error, loadList, loadBySlug }
})
