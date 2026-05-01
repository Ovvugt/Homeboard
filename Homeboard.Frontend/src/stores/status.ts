import { defineStore } from 'pinia'
import { ref } from 'vue'
import { statusApi } from '@/api/status'
import type { TileStatusSnapshot } from '@/types/board'

const POLL_MS = 10_000

export const useStatusStore = defineStore('status', () => {
  const byTile = ref<Map<string, TileStatusSnapshot>>(new Map())
  let timer: ReturnType<typeof setInterval> | null = null
  let activeBoardId: string | null = null

  async function refresh(boardId: string) {
    try {
      const list = await statusApi.listByBoard(boardId)
      const next = new Map<string, TileStatusSnapshot>()
      for (const s of list) next.set(s.tileId, s)
      byTile.value = next
    } catch (e) {
      console.warn('status refresh failed', e)
    }
  }

  function start(boardId: string) {
    if (activeBoardId === boardId && timer) return
    stop()
    activeBoardId = boardId
    refresh(boardId)
    timer = setInterval(() => {
      if (activeBoardId) refresh(activeBoardId)
    }, POLL_MS)
  }

  function stop() {
    if (timer) {
      clearInterval(timer)
      timer = null
    }
    activeBoardId = null
  }

  function get(tileId: string): TileStatusSnapshot | undefined {
    return byTile.value.get(tileId)
  }

  return { byTile, start, stop, refresh, get }
})
