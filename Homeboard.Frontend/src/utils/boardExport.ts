import { boardsApi } from '@/api/boards'
import { tilesApi } from '@/api/tiles'
import { widgetsApi } from '@/api/widgets'
import { ApiError } from '@/api/client'
import type {
  BoardDetail,
  CreateTileDto,
  CreateWidgetDto,
  TileDto,
  WidgetDto,
} from '@/types/board'

export const EXPORT_VERSION = 1

export interface ExportedTile extends Omit<TileDto, 'id' | 'boardId'> {}
export interface ExportedWidget extends Omit<WidgetDto, 'id' | 'boardId'> {}

export interface ExportedBoard {
  name: string
  slug: string
  sortOrder: number
  gridColumns: number
  tiles: ExportedTile[]
  widgets: ExportedWidget[]
}

export interface BoardExport {
  version: number
  exportedAt: string
  boards: ExportedBoard[]
}

export interface ImportResult {
  created: string[]
  replaced: string[]
  skipped: { slug: string; reason: string }[]
  failed: { slug: string; reason: string }[]
}

export interface ImportOptions {
  overwrite?: boolean
}

function stripTile(t: TileDto): ExportedTile {
  const { id: _id, boardId: _bid, ...rest } = t
  return rest
}

function stripWidget(w: WidgetDto): ExportedWidget {
  const { id: _id, boardId: _bid, ...rest } = w
  return rest
}

export async function buildExport(): Promise<BoardExport> {
  const summaries = await boardsApi.list()
  const sorted = [...summaries].sort((a, b) => a.sortOrder - b.sortOrder)
  const details = await Promise.all(sorted.map(s => boardsApi.get(s.slug)))
  const boards: ExportedBoard[] = details.map((d: BoardDetail) => ({
    name: d.name,
    slug: d.slug,
    sortOrder: d.sortOrder,
    gridColumns: d.gridColumns,
    tiles: d.tiles.map(stripTile),
    widgets: d.widgets.map(stripWidget),
  }))
  return {
    version: EXPORT_VERSION,
    exportedAt: new Date().toISOString(),
    boards,
  }
}

export function downloadExport(data: BoardExport, filename = 'homeboard-export.json') {
  const blob = new Blob([JSON.stringify(data, null, 2)], { type: 'application/json' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = filename
  document.body.appendChild(a)
  a.click()
  document.body.removeChild(a)
  URL.revokeObjectURL(url)
}

export function parseExport(text: string): BoardExport {
  let parsed: unknown
  try {
    parsed = JSON.parse(text)
  } catch (e) {
    throw new Error(`Not valid JSON: ${e instanceof Error ? e.message : String(e)}`)
  }
  if (!parsed || typeof parsed !== 'object') {
    throw new Error('Export file must be a JSON object.')
  }
  const obj = parsed as Partial<BoardExport>
  if (typeof obj.version !== 'number') {
    throw new Error('Missing "version" field.')
  }
  if (obj.version !== EXPORT_VERSION) {
    throw new Error(`Unsupported export version ${obj.version} (expected ${EXPORT_VERSION}).`)
  }
  if (!Array.isArray(obj.boards)) {
    throw new Error('Missing "boards" array.')
  }
  return obj as BoardExport
}

export async function importBoards(
  data: BoardExport,
  options: ImportOptions = {},
): Promise<ImportResult> {
  const result: ImportResult = { created: [], replaced: [], skipped: [], failed: [] }
  const existing = await boardsApi.list()
  const existingBySlug = new Map(existing.map(b => [b.slug, b]))

  for (const board of data.boards) {
    const conflict = existingBySlug.get(board.slug)
    const isReplace = !!conflict && !!options.overwrite
    if (conflict && !options.overwrite) {
      result.skipped.push({ slug: board.slug, reason: 'A board with this slug already exists.' })
      continue
    }
    try {
      if (isReplace && conflict) {
        await boardsApi.delete(conflict.id)
      }
      const created = await boardsApi.create({
        name: board.name,
        slug: board.slug,
        gridColumns: board.gridColumns,
      })
      for (const t of board.tiles) {
        const dto: CreateTileDto = { ...t, boardId: created.id }
        await tilesApi.create(dto)
      }
      for (const w of board.widgets) {
        const dto: CreateWidgetDto = { ...w, boardId: created.id }
        await widgetsApi.create(dto)
      }
      if (isReplace) result.replaced.push(board.slug)
      else result.created.push(board.slug)
    } catch (e) {
      const reason = e instanceof ApiError
        ? `${e.status} ${e.statusText}`
        : e instanceof Error ? e.message : String(e)
      result.failed.push({ slug: board.slug, reason })
    }
  }
  return result
}
