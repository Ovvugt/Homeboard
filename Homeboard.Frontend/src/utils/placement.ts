export interface GridRect {
  gridX: number
  gridY: number
  gridW: number
  gridH: number
}

/**
 * Find the first free top-left cell that fits a `w × h` rectangle on a `columns`-wide grid.
 * Scans row-by-row, left-to-right (so new items wrap horizontally and only spill to a new
 * row when no horizontal space is left).
 */
export function findFreeSpot(
  items: readonly GridRect[],
  columns: number,
  w: number,
  h: number,
): { x: number; y: number } {
  const fitW = Math.min(w, columns)
  const maxRow = items.reduce((m, it) => Math.max(m, it.gridY + it.gridH), 0)
  for (let y = 0; y <= maxRow; y++) {
    for (let x = 0; x + fitW <= columns; x++) {
      if (!collides(items, x, y, fitW, h)) return { x, y }
    }
  }
  return { x: 0, y: maxRow }
}

function collides(items: readonly GridRect[], x: number, y: number, w: number, h: number): boolean {
  for (const it of items) {
    if (
      x < it.gridX + it.gridW &&
      x + w > it.gridX &&
      y < it.gridY + it.gridH &&
      y + h > it.gridY
    ) return true
  }
  return false
}
