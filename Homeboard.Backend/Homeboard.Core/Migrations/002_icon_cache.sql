CREATE TABLE icon_cache (
    host         TEXT PRIMARY KEY,
    content_type TEXT NOT NULL,
    bytes        BLOB NOT NULL,
    source_url   TEXT NOT NULL,
    fetched_utc  TEXT NOT NULL,
    failed       INTEGER NOT NULL DEFAULT 0
);
