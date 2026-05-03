CREATE TABLE sections (
    id           TEXT PRIMARY KEY,
    board_id     TEXT NOT NULL REFERENCES boards(id) ON DELETE CASCADE,
    parent_id    TEXT REFERENCES sections(id) ON DELETE CASCADE,
    name         TEXT,
    sort_order   INTEGER NOT NULL DEFAULT 0,
    collapsed    INTEGER NOT NULL DEFAULT 0,
    created_utc  TEXT NOT NULL,
    updated_utc  TEXT NOT NULL
);
CREATE INDEX ix_sections_board ON sections(board_id);
CREATE INDEX ix_sections_parent ON sections(parent_id);

ALTER TABLE tiles   ADD COLUMN section_id TEXT REFERENCES sections(id) ON DELETE SET NULL;
ALTER TABLE widgets ADD COLUMN section_id TEXT REFERENCES sections(id) ON DELETE SET NULL;

CREATE INDEX ix_tiles_section   ON tiles(section_id);
CREATE INDEX ix_widgets_section ON widgets(section_id);

-- One implicit root section per existing board (NULL parent_id, NULL name).
INSERT INTO sections (id, board_id, parent_id, name, sort_order, collapsed, created_utc, updated_utc)
SELECT
    lower(
        substr(hex(randomblob(4)), 1, 8) || '-' ||
        substr(hex(randomblob(2)), 1, 4) || '-' ||
        substr(hex(randomblob(2)), 1, 4) || '-' ||
        substr(hex(randomblob(2)), 1, 4) || '-' ||
        substr(hex(randomblob(6)), 1, 12)
    ),
    id, NULL, NULL, 0, 0,
    strftime('%Y-%m-%dT%H:%M:%fZ','now'),
    strftime('%Y-%m-%dT%H:%M:%fZ','now')
FROM boards;

UPDATE tiles
   SET section_id = (
       SELECT s.id FROM sections s
        WHERE s.board_id = tiles.board_id AND s.parent_id IS NULL
        LIMIT 1
   );

UPDATE widgets
   SET section_id = (
       SELECT s.id FROM sections s
        WHERE s.board_id = widgets.board_id AND s.parent_id IS NULL
        LIMIT 1
   );
