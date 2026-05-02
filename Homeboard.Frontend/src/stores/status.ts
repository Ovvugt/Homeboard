import { defineStore } from 'pinia'
import { ref } from 'vue'
import { statusApi } from '@/api/status'
import type { TileStatusHistoryPoint, TileStatusSnapshot } from '@/types/board'

const POLL_MS = 10_000
const HISTORY_CAP = 500

export const useStatusStore = defineStore('status', () => {
  const byTile = ref<Map<string, TileStatusSnapshot>>(new Map())
  const historyByTile = ref<Map<string, TileStatusHistoryPoint[]>>(new Map())
  let timer: ReturnType<typeof setInterval> | null = null
  let activeBoardId: string | null = null

  async function refresh(boardId: string) {
    try {
      const list = await statusApi.listByBoard(boardId)
      const next = new Map<string, TileStatusSnapshot>()
      for (const s of list) next.set(s.tileId, s)
      byTile.value = next
      appendNewSnapshotsToHistory(list)
    } catch (e) {
      console.warn('status refresh failed', e)
    }
  }

  async function loadHistory(boardId: string) {
    try {
      const points = await statusApi.historyByBoard(boardId)
      const next = new Map<string, TileStatusHistoryPoint[]>()
      for (const p of points) {
        const arr = next.get(p.tileId) ?? []
        arr.push(p)
        next.set(p.tileId, arr)
      }
      historyByTile.value = next
    } catch (e) {
      console.warn('status history load failed', e)
    }
  }

  function appendNewSnapshotsToHistory(snapshots: TileStatusSnapshot[]) {
    const next = new Map(historyByTile.value)
    let changed = false
    for (const snap of snapshots) {
      const arr = next.get(snap.tileId) ?? []
      const last = arr.length > 0 ? arr[arr.length - 1] : null
      if (last && last.checkedUtc === snap.lastCheckedUtc) continue
      const point: TileStatusHistoryPoint = {
        tileId: snap.tileId,
        checkedUtc: snap.lastCheckedUtc,
        status: snap.status,
        responseTimeMs: snap.responseTimeMs,
      }
      const updated = [...arr, point]
      if (updated.length > HISTORY_CAP) updated.splice(0, updated.length - HISTORY_CAP)
      next.set(snap.tileId, updated)
      changed = true
    }
    if (changed) historyByTile.value = next
  }

  async function start(boardId: string) {
    if (activeBoardId === boardId && timer) return
    stop()
    activeBoardId = boardId
    await loadHistory(boardId)
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

  function history(tileId: string): TileStatusHistoryPoint[] {
    return historyByTile.value.get(tileId) ?? []
  }

  return { byTile, historyByTile, start, stop, refresh, get, history }
})
