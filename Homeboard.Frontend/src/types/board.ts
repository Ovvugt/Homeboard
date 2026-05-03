export type TileIconKind = 'Url' | 'Initials' | 'Builtin'
export type TileStatusType = 'None' | 'HttpHead' | 'HttpGet' | 'Tcp'
export type WidgetType = 'Clock' | 'Weather' | 'Minecraft'
export type LayoutItemKind = 'Tile' | 'Widget'
export type StatusValue = 'Up' | 'Down' | 'Unknown'

export interface BoardSummary {
  id: string
  name: string
  slug: string
  sortOrder: number
  gridColumns: number
}

export interface SectionDto {
  id: string
  boardId: string
  parentId: string | null
  name: string | null
  sortOrder: number
  collapsed: boolean
}

export interface BoardDetail extends BoardSummary {
  sections: SectionDto[]
  tiles: TileDto[]
  widgets: WidgetDto[]
}

export interface TileDto {
  id: string
  boardId: string
  sectionId: string | null
  name: string
  url: string
  iconUrl: string | null
  iconKind: TileIconKind
  description: string | null
  color: string | null
  gridX: number
  gridY: number
  gridW: number
  gridH: number
  statusType: TileStatusType
  statusTarget: string | null
  statusInterval: number
  statusTimeout: number
  statusExpected: number | null
}

export interface WidgetDto {
  id: string
  boardId: string
  sectionId: string | null
  type: WidgetType
  gridX: number
  gridY: number
  gridW: number
  gridH: number
  configJson: string
}

export interface CreateBoardDto {
  name: string
  slug: string
  gridColumns?: number
}

export interface CreateTileDto {
  boardId: string
  sectionId?: string | null
  name: string
  url: string
  iconUrl?: string | null
  iconKind: TileIconKind
  description?: string | null
  color?: string | null
  gridX: number
  gridY: number
  gridW: number
  gridH: number
  statusType: TileStatusType
  statusTarget?: string | null
  statusInterval?: number
  statusTimeout?: number
  statusExpected?: number | null
}

export interface UpdateTileDto {
  name: string
  url: string
  iconUrl?: string | null
  iconKind: TileIconKind
  description?: string | null
  color?: string | null
  statusType: TileStatusType
  statusTarget?: string | null
  statusInterval: number
  statusTimeout: number
  statusExpected?: number | null
}

export interface CreateWidgetDto {
  boardId: string
  sectionId?: string | null
  type: WidgetType
  gridX: number
  gridY: number
  gridW: number
  gridH: number
  configJson?: string
}

export interface CreateSectionDto {
  boardId: string
  parentId?: string | null
  name?: string | null
}

export interface UpdateSectionDto {
  name?: string | null
  parentId?: string | null
  sortOrder: number
  collapsed: boolean
}

export interface LayoutItem {
  id: string
  kind: LayoutItemKind
  sectionId?: string | null
  gridX: number
  gridY: number
  gridW: number
  gridH: number
}

export interface SaveLayoutDto {
  items: LayoutItem[]
}

export interface TileStatusSnapshot {
  tileId: string
  status: StatusValue
  lastCheckedUtc: string
  lastUpUtc: string | null
  lastDownUtc: string | null
  responseTimeMs: number | null
  note: string | null
}

export interface TileStatusHistoryPoint {
  tileId: string
  checkedUtc: string
  status: StatusValue
  responseTimeMs: number | null
}
