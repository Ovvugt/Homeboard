import { request } from './client'
import type { TileStatusHistoryPoint, TileStatusSnapshot } from '@/types/board'

export const statusApi = {
  listByBoard: (boardId: string) =>
    request<TileStatusSnapshot[]>('/api/status', { params: { boardId } }),
  historyByBoard: (boardId: string, max = 240) =>
    request<TileStatusHistoryPoint[]>('/api/status/history', { params: { boardId, max } }),
  checkNow: (tileId: string) =>
    request<TileStatusSnapshot>(`/api/status/${tileId}/check`, { method: 'POST' }),
}
