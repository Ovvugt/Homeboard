import { portabilityApi } from '@/api/portability'

export const EXPORT_VERSION = 1
const SUPPORTED_VERSIONS = [1] as const

// Mirrors Homeboard.Boards.Dtos.BoardExportDto on the backend. Kept here so the
// frontend can do lightweight slug-based conflict detection before posting,
// but the heavy lifting (validation, transactional insert) lives server-side.

export interface ExportedSection {
  localId: string
  parentLocalId: string | null
  name: string | null
  sortOrder: number
  collapsed: boolean
}

export interface ExportedTile {
  sectionLocalId: string | null
  name: string
  url: string
  iconUrl: string | null
  iconKind: string
  description: string | null
  color: string | null
  gridX: number
  gridY: number
  gridW: number
  gridH: number
  statusType: string
  statusTarget: string | null
  statusInterval: number
  statusTimeout: number
  statusExpected: number | null
}

export interface ExportedWidget {
  sectionLocalId: string | null
  type: string
  gridX: number
  gridY: number
  gridW: number
  gridH: number
  configJson: string
}

export interface ExportedBoard {
  name: string
  slug: string
  sortOrder: number
  gridColumns: number
  sections: ExportedSection[]
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

export function buildExport(): Promise<BoardExport> {
  return portabilityApi.export()
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

// Lightweight client-side parse. The backend re-validates and is the source of truth;
// this only needs enough structure for the conflict-detection modal to list slugs
// before we post the file.
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
  if (!SUPPORTED_VERSIONS.includes(obj.version as typeof SUPPORTED_VERSIONS[number])) {
    throw new Error(
      `Unsupported export version ${obj.version}. This build supports version(s) ${SUPPORTED_VERSIONS.join(', ')} — please update Homeboard and try again.`,
    )
  }
  if (!Array.isArray(obj.boards)) {
    throw new Error('Missing "boards" array.')
  }
  return obj as BoardExport
}

export function importBoards(data: BoardExport, options: ImportOptions = {}): Promise<ImportResult> {
  return portabilityApi.import(data, !!options.overwrite)
}
