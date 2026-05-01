import { request } from './client'
import type { TileStatusSnapshot } from '@/types/board'

export const statusApi = {
  listByBoard: (boardId: string) =>
    request<TileStatusSnapshot[]>('/api/status', { params: { boardId } }),
  checkNow: (tileId: string) =>
    request<TileStatusSnapshot>(`/api/status/${tileId}/check`, { method: 'POST' }),
}
