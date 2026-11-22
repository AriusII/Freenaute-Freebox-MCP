# Serveur MCP Freebox — présentation (français)

Vous êtes connecté à `Freenaute.Freebox.Mcp`, un serveur MCP **local en stdio** qui dialogue avec une Freebox Server
via l'API Freebox OS (package NuGet `Freenaute.Freebox.Client`). Il offre la **découverte, l'appairage, les
consultations réseaux et système**, ainsi que des vues bornées sur les
téléchargements, appels, contacts, caméras, notifications, AirMedia, contrôle parental et espaces de partage.
L'interface MCP expose aussi deux opérations de gestion du journal des appels ; elle ne permet pas de changer la
configuration générale de la Freebox.

## Transport et exécution

- stdio uniquement : JSON-RPC sur stdin/stdout ; les logs passent sur stderr. Aucun port TCP n'est ouvert.
- .NET 11, binaire Native AOT single-file ; aussi distribué comme outil NuGet (`Freenaute.Freebox.Mcp`) exécutable via
  `dnx` ou `freenaute-freebox-mcp`.
- Identité serveur : `Freenaute.Freebox.Mcp` (version `1.0.0`) ; nom de package dans `.mcp/server.json` :
  `org.freenaute/freenaute-freebox-mcp`.

## Capacités

31 outils (voir `freebox://docs/fr/resources` pour le détail et des exemples), organisés en 12 familles :

- **Découverte** : `freebox_discover` vérifie que la Freebox est joignable et retourne des métadonnées limitées de
  l'API (lecture seule).
- **Appairage** : `freebox_pairing_status` lit l'état des identifiants persistés ; `freebox_pairing_initiate`
  démarre l'appairage seulement avec `FREEBOX_ALLOW_PAIRING_INITIATION=1|true|TRUE` ; `freebox_pairing_wait`
  attend l'approbation physique avec des délais optionnels.
- **Réseau** : `freebox_connection_status` décrit la connexion et les compteurs ; `freebox_lan_overview` résume
  le LAN ; `freebox_wifi_status` décrit l'état Wi-Fi.
- **Système** : `freebox_system_status` fournit le firmware, l'uptime, l'état du disque, les capteurs, ventilateurs
  et emplacements d'extension (lecture seule).
- **Téléchargements** : liste les tâches, leur état, statistiques, configuration, fichiers, journal et morceaux.
- **Appels** : consulte le journal et le compte téléphonique ; peut marquer les appels comme lus ou vider le journal.
- **Contacts** : liste ou recherche des résumés de contacts sans détails sensibles.
- **Caméras / domicile** : résume les caméras, ainsi que langue, veille et mises à jour du domicile.
- **Notifications** : agrège les flux d'état et les notifications courantes.
- **AirMedia** : consulte l'état du diffuseur et ses récepteurs.
- **Contrôle parental** : consulte la configuration et les filtres existants.
- **Partages** : consulte la visibilité du NAS et des partages réseau.

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
   dans le coffre d'identifiants de l'OS (Credential Manager Windows / Secret Service Linux via `secret-tool` /
   Trousseau macOS) ; un conteneur ou hôte headless peut utiliser le backend fichier chiffré quand ses variables sont
   configurées. Si aucun stockage sécurisé n'est disponible, le serveur échoue en « fail-closed ». Le jeton de session
   reste uniquement en mémoire (en-tête `X-Fbx-App-Auth`).

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
  un argument, une variable d'environnement, un fichier de configuration ou un log. Ce serveur n'expose volontairement
  aucun chemin CLI d'import de jeton : utilisez uniquement les outils d'appairage.
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

| Variable                                | Défaut                          | Rôle                                                                                |
|-----------------------------------------|---------------------------------|-------------------------------------------------------------------------------------|
| `FREEBOX_APPLICATION_ID`                | `org.freenaute.freebox-mcp`     | Identifiant d'application Freebox.                                                  |
| `FREEBOX_APPLICATION_NAME`              | `Freenaute Freebox MCP`         | Nom affiché pendant l'appairage.                                                    |
| `FREEBOX_DEVICE_NAME`                   | nom de la machine               | Nom de l'appareil affiché pendant l'appairage.                                      |
| `FREEBOX_CREDENTIAL_PROFILE`            | `default`                       | Profil d'identifiants (portée du stockage).                                         |
| `FREEBOX_ALLOW_PAIRING_INITIATION`      | non défini = désactivé          | `1`/`true`/`TRUE` pour autoriser `freebox_pairing_initiate`.                        |
| `FREEBOX_SERVER_ADDRESS`                | `https://mafreebox.freebox.fr/` | Origine HTTP(S) absolue de l'API, sans chemin ni identifiants.                      |
| `FREEBOX_PAIRING_POLL_INTERVAL_SECONDS` | `2`                             | Intervalle par défaut de `freebox_pairing_wait` ; doit être > 0.                    |
| `FREEBOX_PAIRING_WAIT_TIMEOUT_SECONDS`  | `600`                           | Délai global par défaut de `freebox_pairing_wait` ; doit être > 0.                  |
| `FREEBOX_AUTHENTICATE_ON_STARTUP`       | `true`                          | Réutilise les identifiants au démarrage et peut poursuivre un appairage en attente. |
| `FREEBOX_CREDENTIAL_FILE`               | non défini                      | Fichier chiffré AES-256-GCM optionnel pour les conteneurs ou les hôtes headless.    |
| `FREEBOX_CREDENTIAL_PASSPHRASE_FILE`    | non défini                      | Fichier contenant la phrase secrète du coffre chiffré monté.                        |
