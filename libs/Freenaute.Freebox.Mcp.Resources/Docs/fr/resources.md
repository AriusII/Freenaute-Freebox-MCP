# Catalogue des ressources et outils (français)

Ce document est servi par le serveur MCP sous `freebox://docs/fr/resources` (et son pendant anglais
`freebox://docs/en/resources`). Utilisez-le pour choisir quel outil ou quelle ressource appeler.

## Ressources MCP

| URI                                            | MIME          | Contenu                                                                                                                                          |
|------------------------------------------------|---------------|--------------------------------------------------------------------------------------------------------------------------------------------------|
| `freebox://docs/fr/overview`                   | text/markdown | Présentation du serveur, déroulé d'appairage, règles de sécurité, limites, variables d'environnement.                                            |
| `freebox://docs/fr/resources`                  | text/markdown | Ce catalogue : outils, paramètres, formats de sortie, exemples.                                                                                  |
| `freebox://docs/en/overview`                   | text/markdown | Version anglaise de la présentation.                                                                                                             |
| `freebox://docs/en/resources`                  | text/markdown | Version anglaise de ce catalogue.                                                                                                                |
| `freebox://docs/fr/quickstart`                 | text/markdown | Démarrage rapide, routage des familles et règles d'exécution.                                                                                    |
| `freebox://docs/fr/authentification-appairage` | text/markdown | Modèle officiel d'appairage, persistance et authentification au démarrage.                                                                       |
| `freebox://docs/en/quickstart`                 | text/markdown | Démarrage rapide anglais.                                                                                                                        |
| `freebox://docs/en/auth-pairing`               | text/markdown | Documentation anglaise d'appairage et d'authentification.                                                                                        |
| `freebox://live/status`                        | text/markdown | Instantané assaini de l'état courant : sections `## Pairing`, `## Network`, `## System` (compteurs bornés, valeurs assainies, tronqué à 64 Kio). |

Les ressources de documentation sont du markdown embarqué statique ; la ressource live est produite via
`IFreeboxLiveResourceSource` avec un délai de 10 s.

## Outils MCP

Familles disponibles :

- **Découverte** : vérifie l'accessibilité et les capacités minimales de la Freebox.
- **Appairage** : consulte, démarre ou attend l'autorisation de l'application.
- **Réseau** : consulte la connexion Internet, le LAN et l'état Wi-Fi.
- **Système** : consulte le matériel, les capteurs, les ventilateurs et les extensions.
- **Téléchargements** : liste les tâches, statuts, statistiques, configuration, fichiers, journaux et morceaux.
- **Appels** : consulte le journal et le compte ; peut marquer les appels comme lus ou vider le journal.
- **Contacts** : liste ou recherche des résumés de contacts.
- **Caméras / domicile** : résume les caméras, la langue, la veille et les mises à jour.
- **Notifications** : agrège les flux d'état et les notifications.
- **AirMedia** : consulte l'état AirMedia et les récepteurs.
- **Contrôle parental** : consulte les filtres et la configuration.
- **Partages** : consulte la visibilité du NAS et des interfaces de partage.

| Outil                       | Lecture seule    | Idempotent | Paramètres                                                                                              | Retourne (contenu structuré)                                                                                                                                                                                                                                                 |
|-----------------------------|------------------|------------|---------------------------------------------------------------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `freebox_discover`          | oui              | oui        | —                                                                                                       | `boxModel` (≤64 cars), `apiVersion` (≤32), `httpsAvailable`, `httpsPort`.                                                                                                                                                                                                    |
| `freebox_pairing_status`    | oui              | oui        | —                                                                                                       | `credentialStoreAvailable`, `credentialState` : `Missing\|Pending\|Approved\|Denied\|Expired\|Corrupt\|Unavailable`.                                                                                                                                                         |
| `freebox_pairing_initiate`  | non (verrouillé) | non        | —                                                                                                       | `initiated`, `state`, `message`. Échoue sauf si `FREEBOX_ALLOW_PAIRING_INITIATION` vaut `1`/`true`/`TRUE`.                                                                                                                                                                   |
| `freebox_pairing_wait`      | non              | oui        | `pollIntervalSeconds` (int?, > 0), `timeoutSeconds` (int?, > 0)                                         | `approved`, `state`, `message`. Attend `Approved`/`Denied`/`Expired`; les valeurs utilisent les délais de l'hôte par défaut. Annulable et reprenable.                                                                                                                        |
| `freebox_connection_status` | oui              | oui        | —                                                                                                       | `state`, `type`, `media`, `rateUp`, `rateDown`, `bandwidthUp`, `bandwidthDown`, `bytesUp`, `bytesDown`, `pingEnabled`, `apiRemoteAccessEnabled`, `remoteAccessEnabled`, `wakeOnLanEnabled`, `sipAlg`. **Aucune adresse IP.**                                                 |
| `freebox_lan_overview`      | oui              | oui        | `includeHosts` (bool, défaut `false`), `interfaceName` (string?, 1–64 cars, sans caractère de contrôle) | `name` (≤64), `type`, `mode`, `interfaces[]` : `name` (≤64), `hostCount`, `hosts[]` optionnel : `id` (hex SHA-256), `hostType` (≤32), `reachable`, `active`, `lastActivity`, `interface` (≤64). **Aucun nom d'hôte, domaine ou adresse.**                                    |
| `freebox_wifi_status`       | oui              | oui        | —                                                                                                       | `enabled`, `macFilterState`, `state`, `accessPoints[]` (≤32) : `id`, `state` (≤32) ; `networks[]` (≤128) : `id` (hex SHA-256), `phyId`, `state` (≤32), `stationCount`, `authorizedStationCount`, `enabled`, `encryption` (≤32), `wpsEnabled`. **Aucun SSID ni BSSID.**       |
| `freebox_system_status`     | oui              | oui        | —                                                                                                       | `firmwareVersion` (≤32), `model` (≤64), `boardName` (≤64), `uptimeSeconds`, `boxAuthenticated`, `usb3Enabled`, `userStoragePowered`, `diskStatus`, `sensors[]`/`fans[]` (≤32) : `id` (hex SHA-256), `value` ; `expansions[]` (≤32) : `slot`, `present`, `supported`, `type`. |

Tous les outils sont `Destructive = false`, `OpenWorld = true`, et renvoient un contenu structuré typé plus le texte
JSON.

## Familles pratiques complètes

### Téléchargements

- `freebox_downloads_list` : jusqu'à 128 tâches, sans chemins ni identifiants sensibles.
- `freebox_download_status` : une tâche identifiée par un entier `id > 0`.
- `freebox_download_stats` : compteurs de tâches, flux RSS, débits et bridage.
- `freebox_download_configuration` : plafonds, surveillance, mode de bridage et DNS assainis.
- `freebox_download_files` : jusqu'à 512 fichiers ; chemins et URL exclues.
- `freebox_download_log` : journal texte borné à 4096 caractères.
- `freebox_download_pieces` : état des morceaux borné à 4096 caractères.

Tous ces outils sont en lecture seule. Ne déduisez pas de chemin privé, de secret ou de nom de fichier à partir des
sorties sanitisées.

### Appels téléphoniques

- `freebox_calls_log` : jusqu'à 128 entrées avec identifiants hachés, sans numéro ni nom.
- `freebox_calls_account` : identifiant haché du compte téléphonique, jamais son numéro.
- `freebox_calls_mark_all_read` : marque toutes les entrées comme lues.
- `freebox_calls_clear_log` : supprime tout le journal ; opération destructive à confirmer selon la politique du client.

Préférez une vue limitée car le journal peut contenir des données personnelles. Ne tentez pas de retrouver l'identité
de l'appelant à partir d'un identifiant haché.

### Contacts

- `freebox_contacts_list` : liste jusqu'à 64 contacts avec `query` facultatif et `limit` de 1 à 64.
- `freebox_contacts_search` : recherche sur les noms affiché, prénom, nom ou société ; `query` doit avoir 1 à
  128 caractères sans caractère de contrôle et `limit` doit être compris entre 1 et 64.

Les sorties donnent uniquement un identifiant haché, un nom affiché borné, la société et les nombres de numéros,
e-mails, adresses et URL.

### Caméras et domicile

- `freebox_cameras_overview` : jusqu'à 32 résumés ; aucun nom, URL de flux ou identifiant LAN brut.
- `freebox_home_status` : agrège langue, veille/configuration de veille et état des mises à jour.

Ces outils sont en lecture seule : ils ne démarrent aucun flux, ne prennent aucune photographie et ne modifient pas
la configuration du domicile.

### Notifications et flux

- `freebox_status_feed` : agrège l'appairage, la connexion, le LAN, le Wi-Fi et la santé système.
- `freebox_notifications_feed` : produit des notifications sur les identifiants, la liaison, le Wi-Fi, le disque et
  l'authentification.

Les sorties sont limitées à 16 notifications ou 8 entrées de statut. Une notification n'est pas une preuve d'incident ;
vérifiez le champ associé avant de conseiller une action.

### AirMedia

- `freebox_airmedia_status` : état AirMedia et capacités des récepteurs.
- `freebox_airmedia_receivers` : jusqu'à 32 récepteurs avec nom borné et capacités photo/audio/vidéo/écran.

Ces outils sont en lecture seule : pas de démarrage de diffusion, d'adresse réseau, de mot de passe ou d'URL.

### Contrôle parental

- `freebox_parental_config` : retourne le mode de filtre par défaut.
- `freebox_parental_filters` : jusqu'à 128 filtres ; `limit` doit être compris entre 1 et 128.

Adresses IP, adresses MAC, noms d'hôte et descriptions brutes sont exclus. Aucun filtre n'est créé, modifié ou
désactivé par ces outils.

### NAS et partages

- `freebox_nas_visibility` : état du stockage utilisateur, disque, firmware et modèle.
- `freebox_shares_visibility` : visibilité des interfaces de partage et compteurs d'hôtes reachable/active.
  `includeHosts=false` est recommandé pour une vue générale ; `interfaceName` doit contenir 1 à 64 caractères sans
  caractère de contrôle.

Ces outils ne modifient pas Samba, FTP, les droits ni les volumes. Les hôtes ne sont pas nommés et leurs
identifiants sont hachés.

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
- Ne demandez jamais le jeton d'application de l'utilisateur dans le chat. Le serveur ne prend en charge que
  l'appairage MCP et rejette les arguments de ligne de commande.
- Si un outil renvoie `CredentialStoreUnavailable` (ex. conteneur Linux), expliquez que l'appairage doit être fait
  sur un hôte doté d'un coffre OS (Windows/macOS) — le serveur échoue en fail-closed par conception.
- `freebox_pairing_wait` peut durer longtemps ; il est annulable — annulez-le si l'utilisateur indique que
  l'approbation a expiré sur la Freebox.
