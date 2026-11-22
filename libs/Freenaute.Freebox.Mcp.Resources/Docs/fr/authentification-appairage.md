# Authentification et appairage Freebox (FR)

Référence officielle : https://dev.freebox.fr/sdk/os/login/

Ce serveur MCP applique le modèle d'authentification Freebox OS et l'expose via des outils MCP sécurisés.

## 1) Modèle Freebox OS (officiel)

### Demande d'autorisation

- Endpoint : `POST /api/v4/login/authorize/`
- Objectif : enregistrer l'application et obtenir :
  - `app_token` (secret durable)
  - `track_id` (identifiant de suivi)
- Contrainte : appel depuis le réseau local, validation physique utilisateur requise.

### Suivi d'autorisation

- Endpoint : `GET /api/v4/login/authorize/{track_id}`
- États possibles :
  - `pending` : en attente
  - `granted` : validé
  - `denied` : refusé
  - `timeout` : délai expiré
  - `unknown` : token invalide/révoqué

### Ouverture de session

- Challenge : `GET /api/v4/login/`
- Session : `POST /api/v4/login/session/`
- Calcul : `HMAC-SHA1(app_token, challenge)`
- Résultat : `session_token` envoyé dans `X-Fbx-App-Auth`

## 2) Correspondance MCP

- `freebox_pairing_status` → lit l'état des credentials durables.
- `freebox_pairing_initiate` → démarre l'autorisation (`/login/authorize/`) selon la politique hôte.
- `freebox_pairing_wait` → scrute (`/login/authorize/{track_id}`) jusqu'à un état terminal.

## 3) Invariants de sécurité côté serveur

- Validation physique sur l'écran Freebox obligatoire.
- Aucun argument de ligne de commande accepté.
- Session token en mémoire uniquement.
- App token stocké uniquement dans stockage sécurisé (coffre OS ou fichier chiffré).
- Sorties MCP assainies et bornées.

## 4) Workflow d'appairage recommandé

1. Appeler `freebox_pairing_status`.
2. Si `Missing` :
   - autoriser l'initiation manuelle via `FREEBOX_ALLOW_PAIRING_INITIATION=1|true|TRUE` si nécessaire.
3. Appeler `freebox_pairing_initiate`.
4. Valider physiquement sur l'écran LCD de la Freebox.
5. Appeler `freebox_pairing_wait` jusqu'à :
   - `Approved` : succès
   - `Denied` : refus utilisateur
   - `Expired` : délai dépassé, relancer le cycle

`freebox_pairing_wait` accepte `pollIntervalSeconds` et `timeoutSeconds` (entiers strictement positifs).

## 5) Authentification au démarrage

Avec `FREEBOX_AUTHENTICATE_ON_STARTUP=true` (défaut), le démarrage :

1. vérifie l'état des credentials persistés,
2. tente une réutilisation automatique,
3. déclenche si besoin un bootstrap d'appairage avec timeout borné.

Le bootstrap ne contourne jamais la validation physique.

## 6) Familles d'erreurs d'authentification

Codes MCP typiques :

- `AuthenticationRequired`
- `AuthorizationPending`
- `AuthorizationDenied`
- `AuthorizationExpired`
- `ApiRejected`
- `CredentialStoreUnavailable`
- `TransportFailure`
- `InvalidRequest`

Les messages sont redactés et ne divulguent pas les secrets.

## 7) Variables d'environnement liées à l'auth

- `FREEBOX_APPLICATION_ID`
- `FREEBOX_APPLICATION_NAME`
- `FREEBOX_DEVICE_NAME`
- `FREEBOX_CREDENTIAL_PROFILE`
- `FREEBOX_ALLOW_PAIRING_INITIATION`
- `FREEBOX_PAIRING_POLL_INTERVAL_SECONDS`
- `FREEBOX_PAIRING_WAIT_TIMEOUT_SECONDS`
- `FREEBOX_AUTHENTICATE_ON_STARTUP`
- `FREEBOX_CREDENTIAL_FILE` / `FREEBOX_CREDENTIAL_PASSPHRASE_FILE` (mode fichier chiffré)
