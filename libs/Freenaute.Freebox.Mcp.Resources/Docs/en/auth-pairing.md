# Freebox authentication and pairing (EN)

## Official model

Freebox OS authentication is based on:

1. App authorization (`/login/authorize`) to obtain an `app_token`
2. Tracking approval state (`/login/authorize/{track_id}`)
3. Session opening (`/login/session`) with HMAC-SHA1(challenge, app_token)

Reference: https://dev.freebox.fr/sdk/os/login/

## MCP mapping

- `freebox_pairing_initiate`: starts authorization when policy allows
- `freebox_pairing_wait`: waits for callback-like approval state transitions
- `freebox_pairing_status`: returns persisted credential state

## Safe operating sequence

1. Call `freebox_pairing_status`.
2. If the state is `Missing`, explain that a new pairing requires host policy opt-in and physical confirmation.
3. Call `freebox_pairing_initiate` only when
   `FREEBOX_ALLOW_PAIRING_INITIATION` is exactly `1`, `true`, or `TRUE`.
4. Ask the user to approve the application on the Freebox LCD. The MCP client cannot approve it remotely.
5. Call `freebox_pairing_wait` with optional positive `pollIntervalSeconds` and `timeoutSeconds` values.
6. Stop with `Approved`, `Denied`, or `Expired`. Resume or retry is safe only for a pending pairing and should not
   bypass user consent.

If `CredentialStoreUnavailable` or `UnsupportedCredentialStorage` is returned, do not provide a fallback token prompt.
Move pairing to a supported host/OS vault or configure the encrypted file backend. Linux containers and other
headless environments remain fail-closed unless secure file storage variables are configured.

## Startup authentication

When `FREEBOX_AUTHENTICATE_ON_STARTUP=true`, the server may reuse a persisted credential and verify its session
without requiring a new pairing. Missing credentials are reported without automatically initiating pairing unless
host policy permits and the startup workflow is designed to do so.

When `false`, startup still reports credential state but does not perform the authentication bootstrap.

## Persistence and platforms

- Windows: Credential Manager
- Linux: secret-tool/libsecret when available
- Headless/container mode: encrypted file backend only when `FREEBOX_CREDENTIAL_FILE` and
  `FREEBOX_CREDENTIAL_PASSPHRASE_FILE` are configured

If secure storage is unavailable, behavior is fail-closed.
