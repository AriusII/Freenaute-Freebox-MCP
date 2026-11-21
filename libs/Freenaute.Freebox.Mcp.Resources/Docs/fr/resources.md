# Catalogue des ressources et outils (français)

Ce document est servi par le serveur MCP sous `freebox://docs/fr/resources` (et son pendant anglais
`freebox://docs/en/resources`). Utilisez-le pour choisir quel outil ou quelle ressource appeler.

## Ressources MCP

| URI                           | MIME          | Contenu                                                                                                                                          |
|-------------------------------|---------------|--------------------------------------------------------------------------------------------------------------------------------------------------|
| `freebox://docs/fr/overview`  | text/markdown | Présentation du serveur, déroulé d'appairage, règles de sécurité, limites, variables d'environnement.                                            |
| `freebox://docs/fr/resources` | text/markdown | Ce catalogue : outils, paramètres, formats de sortie, exemples.                                                                                  |
| `freebox://docs/en/overview`  | text/markdown | Version anglaise de la présentation.                                                                                                             |
| `freebox://docs/en/resources` | text/markdown | Version anglaise de ce catalogue.                                                                                                                |
| `freebox://live/status`       | text/markdown | Instantané assaini de l'état courant : sections `## Pairing`, `## Network`, `## System` (compteurs bornés, valeurs assainies, tronqué à 64 Kio). |

Les ressources de documentation sont du markdown embarqué statique ; la ressource live est produite via
`IFreeboxLiveResourceSource` avec un délai de 10 s.

## Outils MCP

| Outil                       | Lecture seule    | Idempotent | Paramètres                                                                                              | Retourne (contenu structuré)                                                                                                                                                                                                                                                 |
|-----------------------------|------------------|------------|---------------------------------------------------------------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `freebox_discover`          | oui              | oui        | —                                                                                                       | `boxModel` (≤64 cars), `apiVersion` (≤32), `httpsAvailable`, `httpsPort`.                                                                                                                                                                                                    |
| `freebox_pairing_status`    | oui              | oui        | —                                                                                                       | `credentialStoreAvailable`, `credentialState` : `Missing\|Pending\|Approved\|Denied\|Expired\|Corrupt\|Unavailable`.                                                                                                                                                         |
| `freebox_pairing_initiate`  | non (verrouillé) | non        | —                                                                                                       | `initiated`, `state`, `message`. Échoue sauf si `FREEBOX_ALLOW_PAIRING_INITIATION` vaut `1`/`true`/`TRUE`.                                                                                                                                                                   |
| `freebox_pairing_wait`      | non              | oui        | —                                                                                                       | `approved`, `state`, `message`. Attend `Approved`/`Denied`/`Expired` ; annulable.                                                                                                                                                                                            |
| `freebox_connection_status` | oui              | oui        | —                                                                                                       | `state`, `type`, `media`, `rateUp`, `rateDown`, `bandwidthUp`, `bandwidthDown`, `bytesUp`, `bytesDown`, `pingEnabled`, `apiRemoteAccessEnabled`, `remoteAccessEnabled`, `wakeOnLanEnabled`, `sipAlg`. **Aucune adresse IP.**                                                 |
| `freebox_lan_overview`      | oui              | oui        | `includeHosts` (bool, défaut `false`), `interfaceName` (string?, 1–64 cars, sans caractère de contrôle) | `name` (≤64), `type`, `mode`, `interfaces[]` : `name` (≤64), `hostCount`, `hosts[]` optionnel : `id` (hex SHA-256), `hostType` (≤32), `reachable`, `active`, `lastActivity`, `interface` (≤64). **Aucun nom d'hôte, domaine ou adresse.**                                    |
| `freebox_wifi_status`       | oui              | oui        | —                                                                                                       | `enabled`, `macFilterState`, `state`, `accessPoints[]` (≤32) : `id`, `state` (≤32) ; `networks[]` (≤128) : `id` (hex SHA-256), `phyId`, `state` (≤32), `stationCount`, `authorizedStationCount`, `enabled`, `encryption` (≤32), `wpsEnabled`. **Aucun SSID ni BSSID.**       |
| `freebox_system_status`     | oui              | oui        | —                                                                                                       | `firmwareVersion` (≤32), `model` (≤64), `boardName` (≤64), `uptimeSeconds`, `boxAuthenticated`, `usb3Enabled`, `userStoragePowered`, `diskStatus`, `sensors[]`/`fans[]` (≤32) : `id` (hex SHA-256), `value` ; `expansions[]` (≤32) : `slot`, `present`, `supported`, `type`. |

Tous les outils sont `Destructive = false`, `OpenWorld = true`, et renvoient un contenu structuré typé plus le texte
JSON.

## Format de sortie et bornes

- Chaque outil renvoie un objet de résultat ; en cas d'échec, un objet d'erreur avec un `code` stable
  (`AuthenticationRequired`, `AuthorizationPending`, `AuthorizationDenied`, `AuthorizationExpired`, `ApiRejected`,
  `TransportFailure`, `CredentialStoreUnavailable`, `Cancelled`, `InvalidRequest`, `Unknown`), un message **redacté**,
  un statut HTTP facultatif et un identifiant de corrélation.
- Assainissement : caractères de contrôle supprimés, chaînes tronquées selon les plafonds par champ, collections
  plafonnées comme listé ci-dessus, identifiants remplacés par des condensés SHA-256 hexadécimaux en minuscules.
- Markdown de `freebox://live/status` : `## Pairing` (disponibilité du coffre + état), `## Network` (état/type/media
  de la connexion, débits, compteurs d'octets, Wi-Fi activé/état, nombre de points d'accès et de réseaux),
  `## System` (uptime, authentifié, état du disque, nombres de capteurs/ventilateurs/expansions). Les valeurs sont
  assainies en `[A-Za-z0-9_.-]` sur ≤ 32 caractères.

## Exemples de requêtes (conversation type)

1. « Ma Freebox est-elle accessible ? » → `freebox_discover`
2. « Sommes-nous appairés ? » → `freebox_pairing_status` → si `Missing` : expliquer que l'appairage exige la variable
   d'opt-in **et** l'approbation physique sur l'écran de la Freebox, puis `freebox_pairing_initiate` +
   `freebox_pairing_wait`.
3. « Quel est mon débit de connexion ? » → `freebox_connection_status` (`rateUp`/`rateDown` en octets/s, plus les
   compteurs d'octets).
4. « Qui est sur mon LAN ? » → `freebox_lan_overview` avec `includeHosts=true` (hôtes sous forme d'identifiants
   hachés avec indicateurs reachable/active ; noms et adresses volontairement absents).
5. « Montre-moi mes réseaux Wi-Fi » → `freebox_wifi_status` (compteurs par réseau, chiffrement, WPS ; les SSID sont
   volontairement absents).
6. « Donne-moi un instantané complet » → lire la ressource `freebox://live/status`.
7. « La box est-elle en bonne santé ? » → `freebox_system_status` (firmware, uptime, disque, capteurs/ventilateurs).

## Bonnes pratiques pour le LLM

- Préférez `freebox://live/status` pour une vue d'ensemble ; utilisez les outils individuels pour une question précise.
- N'essayez jamais d'exfiltrer des identifiants : les sorties sont hachées par conception ; ne tentez pas de les
  inverser.
- Ne demandez jamais le jeton d'application de l'utilisateur dans le chat ; orientez-le vers
  `freenaute-freebox-mcp import-token --freebox-uid <uid>` (stdin uniquement) s'il s'est déjà appairé ailleurs.
- Si un outil renvoie `CredentialStoreUnavailable` (ex. conteneur Linux), expliquez que l'appairage doit être fait
  sur un hôte doté d'un coffre OS (Windows/macOS) — le serveur échoue en fail-closed par conception.
- `freebox_pairing_wait` peut durer longtemps ; il est annulable — annulez-le si l'utilisateur indique que
  l'approbation a expiré sur la Freebox.
