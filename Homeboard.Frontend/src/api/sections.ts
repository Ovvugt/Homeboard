import { request } from './client'
import type { CreateSectionDto, SectionDto, UpdateSectionDto } from '@/types/board'

export const sectionsApi = {
  create: (dto: CreateSectionDto) =>
    request<SectionDto>('/api/sections', { method: 'POST', body: dto }),
  update: (id: string, dto: UpdateSectionDto) =>
    request<void>(`/api/sections/${id}`, { method: 'PUT', body: dto }),
  delete: (id: string) =>
    request<void>(`/api/sections/${id}`, { method: 'DELETE' }),
}
