# Advanced files domain

The files domain covers downloads and NAS/share visibility. Use read operations for inventory and summaries.
Destructive or write operations require a separate explicit mutation workflow.

## Download tools

- `freebox_downloads_list`, `freebox_download_status`, `freebox_download_stats`
- `freebox_download_configuration`
- `freebox_download_files`, `freebox_download_log`, `freebox_download_pieces`

All download details are bounded and sanitized. Paths, credentials, hashes, passwords, preview URLs, and raw logs
are excluded or truncated.

## Storage and shares

- `freebox_nas_visibility`: storage power, disk, model, and firmware status.
- `freebox_shares_visibility`: bounded interface and share visibility.

Use task IDs from list/status before requesting task details. `freebox://live/files-advanced` is a bounded live
summary and returns a safe fallback if data is unavailable.
