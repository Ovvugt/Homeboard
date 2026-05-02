CREATE TABLE tile_status_history (
    id               INTEGER PRIMARY KEY AUTOINCREMENT,
    tile_id          TEXT NOT NULL REFERENCES tiles(id) ON DELETE CASCADE,
    checked_utc      TEXT NOT NULL,
    status           TEXT NOT NULL,
    response_time_ms INTEGER
);
CREATE INDEX ix_tile_status_history_tile_time ON tile_status_history(tile_id, checked_utc);
