# Serveur MCP Freebox — présentation (français)

Vous êtes connecté à `Freenaute.Freebox.Mcp`, un serveur MCP **local en stdio** qui dialogue avec une Freebox Server
via l'API Freebox OS (package NuGet `Freenaute.Freebox.Client`). Il offre la **découverte, la coordination de
l'appairage et un statut en lecture seule** de la Freebox et de son LAN/Wi-Fi. Il ne peut pas modifier la configuration
de la Freebox.

## Transport et exécution

- stdio uniquement : JSON-RPC sur stdin/stdout ; les logs passent sur stderr. Aucun port TCP n'est ouvert.
- .NET 11, binaire Native AOT single-file ; aussi distribué comme outil NuGet (`Freenaute.Freebox.Mcp`) exécutable via
  `dnx` ou `freenaute-freebox-mcp`.
- Identité serveur : `Freenaute.Freebox.Mcp` (version `0.1.0-beta`) ; nom de package dans `.mcp/server.json` :
  `io.github.freenaute/freenaute-freebox-mcp`.

## Capacités

8 outils (voir `freebox://docs/fr/resources` pour le détail et des exemples) :

- `freebox_discover` — Freebox accessible + métadonnées de capacité de l'API (lecture seule).
- `freebox_pairing_status` — état des identifiants, jamais de secrets (lecture seule).
- `freebox_pairing_initiate` — démarre l'appairage ; **verrouillé** par `FREEBOX_ALLOW_PAIRING_INITIATION=1|true|TRUE`.
- `freebox_pairing_wait` — attend l'approbation d'un appairage existant ; annulable.
- `freebox_connection_status`, `freebox_lan_overview`, `freebox_wifi_status`, `freebox_system_status` — statut
  assaini, en lecture seule.

5 ressources : cette documentation (`freebox://docs/fr/*`, `freebox://docs/en/*`) et `freebox://live/status`.

## Déroulé de l'appairage (important)

1. Appelez `freebox_pairing_status`. Sur une machine neuve, l'état est `Missing`.
2. Si la politique de l'hôte l'autorise, appelez `freebox_pairing_initiate`. L'outil ne s'exécute que si l'opérateur
   a défini `FREEBOX_ALLOW_PAIRING_INITIATION` à exactement `1`, `true` ou `TRUE` ; sinon il échoue avec une erreur de
   politique.
3. **Un humain doit approuver physiquement l'application sur l'écran (LCD) de la Freebox.** C'est obligatoire et
   aucun outil ni aucun réglage ne peut le contourner.
4. Appelez `freebox_pairing_wait` pour attendre l'état `Approved` (ou `Denied` / `Expired`). On peut l'annuler et le
   reprendre sans danger.
5. Une fois `Approved`, les outils de statut en lecture seule fonctionnent. Le jeton d'application durable est stocké
   **chiffré dans le coffre d'identifiants de l'OS** (Credential Manager Windows / Trousseau macOS) ; sur
   Linux/conteneur il n'y a **aucun stockage** et l'appairage échoue en « fail-closed ». Le jeton de session reste
   uniquement en mémoire (en-tête `X-Fbx-App-Auth`).

En interne : `POST /api/v*/login/authorize/` → `app_token` + `track_id` ; scrutation de
`GET /api/v*/login/authorize/{track_id}` (`pending|granted|denied|timeout`) ; puis `GET /api/v*/login/` → `challenge` ;
`POST /api/v*/login/session/` avec `password = HMAC-SHA1(app_token, challenge)` → `session_token`.

## Règles de sécurité (toujours applicables)

- **Lecture seule** : les 6 outils de découverte/statut ne modifient jamais la Freebox. Seuls les deux outils
  d'appairage ne sont pas en lecture seule, et `freebox_pairing_initiate` est verrouillé par la politique.
- **Sorties assainies** : les résultats d'outils et `freebox://live/status` ne contiennent jamais de noms d'hôte,
  de domaine, d'adresses IP ou MAC, de SSID, de BSSID, de jeton ou de challenge. Les identifiants d'hôte, de capteur
  et de BSS sont des condensés SHA-256 hexadécimaux. Les chaînes sont nettoyées des caractères de contrôle et
  tronquées ; les collections sont plafonnées (32 interfaces, 128 hôtes/interface, 32 points d'accès, 128 BSS,
  32 composants système) ; le contenu des ressources est tronqué à 64 Kio.
- **Secrets** : ne demandez jamais à l'utilisateur de coller un jeton dans le chat, et ne placez jamais un jeton dans
  un argument, une variable d'environnement, un fichier de configuration ou un log. Utilisez
  `freenaute-freebox-mcp import-token --freebox-uid <uid>` qui lit le jeton uniquement depuis stdin.
- **Erreurs** : elles reviennent sous forme d'erreur structurée avec un code stable : `AuthenticationRequired`,
  `AuthorizationPending`, `AuthorizationDenied`, `AuthorizationExpired`, `ApiRejected`, `TransportFailure`,
  `CredentialStoreUnavailable`, `Cancelled`, `InvalidRequest`. Les messages sont redactés.

## Limites

- Une Freebox par instance de serveur ; l'adresse par défaut est `https://mafreebox.freebox.fr/`.
- `freebox_lan_overview` ne retourne les hôtes qu'avec `includeHosts=true`, filtrable par `interfaceName`
  (1–64 caractères, sans caractère de contrôle).
- Si le coffre d'identifiants est indisponible (ex. conteneur Linux), les outils de statut indiquent
  `credential state: Unavailable` et les outils d'appairage échouent en fail-closed.
- Les lectures de la ressource live ont un délai de 10 s et sont annulables.

## Configuration (variables d'environnement)

| Variable                           | Défaut                            | Rôle                                                         |
|------------------------------------|-----------------------------------|--------------------------------------------------------------|
| `FREEBOX_APPLICATION_ID`           | `io.github.freenaute.freebox-mcp` | Identifiant d'application Freebox.                           |
| `FREEBOX_APPLICATION_NAME`         | `Freenaute Freebox MCP`           | Nom affiché pendant l'appairage.                             |
| `FREEBOX_DEVICE_NAME`              | nom de la machine                 | Nom de l'appareil affiché pendant l'appairage.               |
| `FREEBOX_CREDENTIAL_PROFILE`       | `default`                         | Profil d'identifiants (portée du stockage).                  |
| `FREEBOX_ALLOW_PAIRING_INITIATION` | non défini = désactivé            | `1`/`true`/`TRUE` pour autoriser `freebox_pairing_initiate`. |
