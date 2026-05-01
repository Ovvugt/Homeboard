import { request } from './client'
import type { CreateWidgetDto, WidgetDto } from '@/types/board'

export const widgetsApi = {
  create: (dto: CreateWidgetDto) => request<WidgetDto>('/api/widgets', { method: 'POST', body: dto }),
  update: (id: string, configJson: string) =>
    request<void>(`/api/widgets/${id}`, { method: 'PUT', body: { configJson } }),
  delete: (id: string) => request<void>(`/api/widgets/${id}`, { method: 'DELETE' }),
}
