# Freebox authentication and pairing (EN)

Official reference: https://dev.freebox.fr/sdk/os/login/

This MCP server implements the Freebox OS authentication model and maps it to safe MCP operations.

## 1) Freebox OS authentication model (official)

### Authorization request

- Endpoint: `POST /api/v4/login/authorize/`
- Goal: register the app and get:
  - `app_token` (durable secret)
  - `track_id` (authorization tracking id)
- Constraint: request must originate from local network and requires user physical access to Freebox display.

### Authorization tracking

- Endpoint: `GET /api/v4/login/authorize/{track_id}`
- Status values:
  - `pending`: user did not confirm yet
  - `granted`: approval accepted, token can be used
  - `denied`: user rejected
  - `timeout`: approval window expired
  - `unknown`: token revoked or invalid

### Session opening

- Challenge endpoint: `GET /api/v4/login/`
- Session endpoint: `POST /api/v4/login/session/`
- Password formula: `HMAC-SHA1(app_token, challenge)`
- Result: `session_token` used in header `X-Fbx-App-Auth`

## 2) MCP mapping

- `freebox_pairing_status` → checks durable credential state.
- `freebox_pairing_initiate` → starts authorization request (`/login/authorize/`) when allowed by host policy.
- `freebox_pairing_wait` → polls approval state (`/login/authorize/{track_id}`) until terminal state.

## 3) Security invariants in this server

- Physical approval on Freebox display is mandatory and cannot be bypassed.
- No launch arguments are accepted by the MCP host.
- Session token is memory-only.
- Durable token is stored in secure storage only (OS vault or encrypted file mode).
- Tool/resource outputs are sanitized and bounded.

## 4) Safe operating sequence

1. Call `freebox_pairing_status`.
2. If state is `Missing`:
   - ensure policy allows initiation (`FREEBOX_ALLOW_PAIRING_INITIATION=1|true|TRUE`) when using manual initiation.
3. Call `freebox_pairing_initiate`.
4. Physically approve on Freebox LCD (physical confirmation is mandatory).
5. Call `freebox_pairing_wait` until:
   - `Approved` → success
   - `Denied` → user rejected
   - `Expired` → timed out, restart flow

The wait call supports optional positive `pollIntervalSeconds` and `timeoutSeconds`.

## 5) Startup authentication behavior

With `FREEBOX_AUTHENTICATE_ON_STARTUP=true` (default), startup does:

1. inspect stored credential state,
2. attempt automatic reuse for approved credentials,
3. if required, initiate/wait pairing bootstrap with bounded timeout policy.

Startup bootstrap still respects physical approval and secure-storage constraints.

## 6) Typical authentication-related error families

Based on Freebox + MCP mapping:

- `AuthenticationRequired`
- `AuthorizationPending`
- `AuthorizationDenied`
- `AuthorizationExpired`
- `ApiRejected`
- `CredentialStoreUnavailable`
- `TransportFailure`
- `InvalidRequest`

All returned messages are redacted and correlation-safe.

## 7) Environment variables affecting auth

- `FREEBOX_APPLICATION_ID`
- `FREEBOX_APPLICATION_NAME`
- `FREEBOX_DEVICE_NAME`
- `FREEBOX_CREDENTIAL_PROFILE`
- `FREEBOX_ALLOW_PAIRING_INITIATION`
- `FREEBOX_PAIRING_POLL_INTERVAL_SECONDS`
- `FREEBOX_PAIRING_WAIT_TIMEOUT_SECONDS`
- `FREEBOX_AUTHENTICATE_ON_STARTUP`
- `FREEBOX_CREDENTIAL_FILE` / `FREEBOX_CREDENTIAL_PASSPHRASE_FILE` (encrypted-file mode)
