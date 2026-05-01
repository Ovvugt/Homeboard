import { request } from './client'
import type { CreateTileDto, TileDto, UpdateTileDto } from '@/types/board'

export const tilesApi = {
  create: (dto: CreateTileDto) => request<TileDto>('/api/tiles', { method: 'POST', body: dto }),
  update: (id: string, dto: UpdateTileDto) => request<void>(`/api/tiles/${id}`, { method: 'PUT', body: dto }),
  delete: (id: string) => request<void>(`/api/tiles/${id}`, { method: 'DELETE' }),
}
