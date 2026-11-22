# Services controls domain

This domain covers Freebox call, contact, notification, AirMedia, and sharing service summaries.

## Read and mutation boundaries

- Calls: log/account tools are read-only; marking read is an explicit mutation; clearing the log is destructive and
  requires confirmation.
- Contacts: list/search only, with bounded results and no raw phone numbers or addresses.
- Notifications: feeds are derived snapshots, not acknowledgment queues.
- AirMedia: status and receiver lists are read-only; no casting or playback operation is exposed.
- Shares: visibility summaries are read-only.

The live URI `freebox://live/services-controls` is bounded and returns safe fallback text when unavailable.
