import { request } from './client'
import type {
  BoardDetail,
  BoardSummary,
  CreateBoardDto,
  SaveLayoutDto,
} from '@/types/board'

export const boardsApi = {
  list: () => request<BoardSummary[]>('/api/boards'),
  get: (slug: string) => request<BoardDetail>(`/api/boards/${slug}`),
  create: (dto: CreateBoardDto) => request<BoardSummary>('/api/boards', { method: 'POST', body: dto }),
  delete: (id: string) => request<void>(`/api/boards/${id}`, { method: 'DELETE' }),
  saveLayout: (boardId: string, dto: SaveLayoutDto) =>
    request<void>(`/api/boards/${boardId}/layout`, { method: 'POST', body: dto }),
}
