import { request } from './client'
import type { BoardExport, ImportResult } from '@/utils/boardExport'

export const portabilityApi = {
  export: () => request<BoardExport>('/api/portability/export'),
  import: (data: BoardExport, overwrite: boolean) =>
    request<ImportResult>('/api/portability/import', {
      method: 'POST',
      body: data,
      params: { overwrite },
    }),
}
