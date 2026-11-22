# System and home controls domain

This domain is for bounded system health and home/camera overview. Current tools are read-only; they do not reboot,
power off, update, cast, or control cameras.

## Tools

- `freebox_system_status`: firmware, model, uptime, disk, authentication, USB, sensors, fans, and expansions.
- `freebox_cameras_overview`: at most 32 sanitized camera summaries.
- `freebox_home_status`: bounded home standby and update state.
- `freebox_status_feed`: at most 8 derived status entries.

Sensor and fan identifiers are hashed. Camera names, LAN identifiers, locations, and stream URLs are excluded.
The live URI `freebox://live/systemhome-controls` provides a bounded summary with safe fallback text.
