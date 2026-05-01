CREATE TABLE boards (
    id            TEXT PRIMARY KEY,
    name          TEXT NOT NULL,
    slug          TEXT NOT NULL UNIQUE,
    sort_order    INTEGER NOT NULL DEFAULT 0,
    grid_columns  INTEGER NOT NULL DEFAULT 12,
    created_utc   TEXT NOT NULL,
    updated_utc   TEXT NOT NULL
);

CREATE TABLE tiles (
    id              TEXT PRIMARY KEY,
    board_id        TEXT NOT NULL REFERENCES boards(id) ON DELETE CASCADE,
    name            TEXT NOT NULL,
    url             TEXT NOT NULL,
    icon_url        TEXT,
    icon_kind       TEXT NOT NULL DEFAULT 'Url',
    description     TEXT,
    color           TEXT,
    grid_x          INTEGER NOT NULL,
    grid_y          INTEGER NOT NULL,
    grid_w          INTEGER NOT NULL,
    grid_h          INTEGER NOT NULL,
    status_type     TEXT NOT NULL DEFAULT 'None',
    status_target   TEXT,
    status_interval INTEGER NOT NULL DEFAULT 60,
    status_timeout  INTEGER NOT NULL DEFAULT 5000,
    status_expected INTEGER
);
CREATE INDEX ix_tiles_board ON tiles(board_id);

CREATE TABLE widgets (
    id          TEXT PRIMARY KEY,
    board_id    TEXT NOT NULL REFERENCES boards(id) ON DELETE CASCADE,
    type        TEXT NOT NULL,
    grid_x      INTEGER NOT NULL,
    grid_y      INTEGER NOT NULL,
    grid_w      INTEGER NOT NULL,
    grid_h      INTEGER NOT NULL,
    config_json TEXT NOT NULL DEFAULT '{}'
);
CREATE INDEX ix_widgets_board ON widgets(board_id);

CREATE TABLE tile_status_snapshots (
    tile_id          TEXT PRIMARY KEY REFERENCES tiles(id) ON DELETE CASCADE,
    status           TEXT NOT NULL,
    last_checked_utc TEXT NOT NULL,
    last_up_utc      TEXT,
    last_down_utc    TEXT,
    response_time_ms INTEGER,
    note             TEXT
);

INSERT INTO boards (id, name, slug, sort_order, grid_columns, created_utc, updated_utc)
VALUES (
    '00000000-0000-0000-0000-000000000001',
    'Home',
    'home',
    0,
    12,
    strftime('%Y-%m-%dT%H:%M:%fZ','now'),
    strftime('%Y-%m-%dT%H:%M:%fZ','now')
);
