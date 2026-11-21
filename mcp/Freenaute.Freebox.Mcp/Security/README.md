# Credential storage and secret handling

This server never stores a Freebox token in plaintext — not in a file, an environment variable, an argument, a log,
or an MCP payload. The durable `app_token` lives **only** in the operating-system credential vault; the
`session_token` lives **only in process memory**.

## Storage backend matrix

Selected at startup by `CredentialStorageRegistration.AddPlatformCredentialStorage()`, first match wins:

| Priority | Condition                          | Backend                                                                                                                                                                                         | `Kind`                           | Behavior                                                                                                                                                                                                    |
|----------|------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|----------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| 1        | `FREEBOX_CREDENTIAL_FILE` set      | `EncryptedFileCredentialStorage`: AES-256-GCM file, key = SHA-256 of the passphrase read from `FREEBOX_CREDENTIAL_PASSPHRASE_FILE`; blob = `FBMCP1` \|\| nonce(12) \|\| tag(16) \|\| ciphertext | `encrypted-file-aes-256-gcm`     | Container/headless mode. Write is verified by reading the file back (`FixedTimeEquals`); decrypt failures and tampering throw `InvalidDataException` without leaking the token.                             |
| 2        | Windows                            | Credential Manager via `CredWriteW` / `CredReadW` / `CredDeleteW` (`advapi32.dll`, `LibraryImport`), `CRED_TYPE_GENERIC`, `CRED_PERSIST_LOCAL_MACHINE`                                          | `windows-credential-manager`     | DPAPI-encrypted blob, local to the Windows logon environment. Available.                                                                                                                                    |
| 3        | Linux with `secret-tool` on `PATH` | `LinuxSecretServiceCredentialStorage`: libsecret through the `secret-tool` CLI (`Process.Start` only, AOT-safe), attributes `service=org.freenaute.freebox-mcp` + `account=<store key>`         | `linux-secret-service-libsecret` | Available when the binary is present; any backend failure throws `NotSupportedException` (fail closed).                                                                                                     |
| 4        | Anything else                      | `UnsupportedCredentialStorage`                                                                                                                                                                  | `unavailable`                    | **Fail-closed**: every operation throws `NotSupportedException` ("Refusing to use plaintext credential storage"). Pairing and `import-token` are unavailable; there is **never** a plaintext JSON fallback. |

## Key scoping

The vault entry name is derived by `CredentialStoreKey.For(FreeboxCredentialId)`:

```text
org.freenaute.freebox-mcp/credentials:<hex>
```

where `<hex>` = lowercase hex of `SHA256( length-prefixed UTF-8( freeboxUid + applicationId + profile ) )`.
The length prefixes prevent ambiguity between concatenated fields. The raw scope and hash buffers are zeroized
(`CryptographicOperations.ZeroMemory`) after use. A credential is therefore scoped by **all three** of: Freebox UID
(discovered via the API), application ID (`FREEBOX_APPLICATION_ID`), and profile (`FREEBOX_CREDENTIAL_PROFILE`).

## Payload format and verification

- The stored blob is **versioned JSON** (format version `1`): application id, app token, status, track id, created /
  updated timestamps — serialized with a **source-generated** `JsonSerializerContext`
  (`CredentialPayloadJsonContext`), so it is Native AOT / trimming safe.
- `FreeboxCredentialStoreAdapter.PutAsync` performs a **read-back verification** after every write (and after delete)
  using a **fixed-time comparison** (`CryptographicOperations.FixedTimeEquals`) on the token bytes; any mismatch throws
  and surfaces as `Corrupt`.
- Buffers holding serialized credentials are zeroized after use.

## What is stored where

| Secret                                                   | Storage                                                                                         |
|----------------------------------------------------------|-------------------------------------------------------------------------------------------------|
| `app_token` (durable, allows re-pairing without the LCD) | OS vault only, as above. Never printed, logged, or returned by any tool.                        |
| `track_id`                                               | Vault payload metadata (part of the credential record); redacted from logs.                     |
| `challenge`, `session_token`                             | **Memory only**. `session_token` is sent as the `X-Fbx-App-Auth` header; it is never persisted. |
| `password = HMAC-SHA1(app_token, challenge)`             | Computed in memory, sent once, never stored or logged.                                          |

## Log redaction

`FreeboxSecretRedactor` rewrites any logged text and replaces the values of `app_token`, `appToken`, `track_id`,
`trackId`, `session_token`, `sessionToken`, `challenge`, `password`, `authorization`, and `cookie` (JSON, quoted and
assignment forms, case-insensitive) with `<redacted>`. Logging is configured with
`LogToStandardErrorThreshold = Trace`, so **stdout is reserved for the MCP protocol** and stderr carries logs only.

## Rules (hard invariants)

1. Never write a token/credential to a plaintext file, env var, argv, MCP config, CI log, or shell history.
2. `import-token` accepts the token from **stdin only** (masked prompt interactively, protected pipe in automation).
   Passing `--token <value>` is rejected with exit code 2 **without echoing the secret**; missing `--freebox-uid` is
   rejected before stdin is read; on unsupported platforms it fails closed (exit code 1) without reading or echoing
   stdin. The stored value is verified by read-back and never printed.
3. Tool outputs are sanitized and bounded: control characters stripped, strings length-capped, collections capped,
   host/sensor/BSS identifiers replaced by SHA-256 hex digests; hostnames, domains, IP/MAC addresses, SSIDs and BSSIDs
   are never returned. Resource contents (including `freebox://live/status`) are truncated at 64 KiB.
4. Pairing initiation is **opt-in** (`FREEBOX_ALLOW_PAIRING_INITIATION` = `1` | `true` | `TRUE`, exact match) and
   **physical approval on the Freebox display is always required** regardless of configuration.
5. Error messages are mapped through `FreeboxErrorMapper` + `FreeboxSecretRedactor`, so API error text is redacted
   before it reaches a tool result or a log.
