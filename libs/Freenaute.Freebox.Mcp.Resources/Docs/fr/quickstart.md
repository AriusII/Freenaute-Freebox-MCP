# Démarrage rapide MCP Freebox (FR)

## Périmètre

Ce serveur MCP est local et en transport stdio uniquement. Il expose des statuts bornés et des workflows d’appairage
sécurisés.

## Flux recommandé

1. `freebox_discover` — vérifier que la Freebox est joignable.
2. `freebox_pairing_status` — vérifier l'état des identifiants persistés.
3. Si nécessaire et autorisé : `freebox_pairing_initiate`.
4. `freebox_pairing_wait` — attendre l'approbation sur l'écran Freebox ; paramètres optionnels
   `pollIntervalSeconds` et `timeoutSeconds`, tous deux strictement positifs.
5. Après appairage, choisir l'une des familles de statuts :
    - réseau : `freebox_connection_status`, `freebox_lan_overview`, `freebox_wifi_status`;
    - système : `freebox_system_status`.
    - autres familles : téléchargements, appels, contacts, caméras, domicile, notifications, AirMedia,
      contrôle parental et visibilité des partages.

Pour une vue globale immédiate, vous pouvez aussi lire `freebox://live/status` plutôt que de chaîner tous les outils.

## Acheminement par objectif

| Objectif                               | Outil privilégié                                                                                                                                                                             | Usage sûr                                                                          |
|----------------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|------------------------------------------------------------------------------------|
| Vérifier l'accessibilité               | `freebox_discover`                                                                                                                                                                           | Requête sans paramètre, répétable.                                                 |
| Vérifier l'appairage                   | `freebox_pairing_status`                                                                                                                                                                     | Ne demande jamais le jeton applicatif.                                             |
| Démarrer ou finir l'appairage          | `freebox_pairing_initiate`, `freebox_pairing_wait`                                                                                                                                           | Opt-in hôte + approbation physique obligatoire.                                    |
| Lire Internet, LAN ou Wi-Fi            | `freebox_connection_status`, `freebox_lan_overview`, `freebox_wifi_status`                                                                                                                   | Les adresses, SSID/BSSID et noms d'hôte sont exclus.                               |
| Lire les statistiques système          | `freebox_system_status`                                                                                                                                                                      | Firmware, uptime, disque, capteurs et ventilateurs bornés.                         |
| Suivre les téléchargements             | `freebox_downloads_list`, `freebox_download_status`, `freebox_download_stats`, `freebox_download_configuration`, `freebox_download_files`, `freebox_download_log`, `freebox_download_pieces` | Tous en lecture seule ; chemins, identifiants sensibles et contenus privés exclus. |
| Consulter ou gérer le journal d'appels | `freebox_calls_log`, `freebox_calls_account`, `freebox_calls_mark_all_read`, `freebox_calls_clear_log`                                                                                       | Les deux dernières opérations modifient le journal ; `clear_log` est destructive.  |
| Rechercher des contacts                | `freebox_contacts_list`, `freebox_contacts_search`                                                                                                                                           | Résumés uniquement, avec limite de 1 à 64.                                         |
| Consulter caméras et domicile          | `freebox_cameras_overview`, `freebox_home_status`                                                                                                                                            | Pas de flux vidéo ni de noms sensibles.                                            |
| Obtenir l'état agrégé                  | `freebox_status_feed`, `freebox_notifications_feed`                                                                                                                                          | Résumés bornés, aucune valeur secrète.                                             |
| Lire AirMedia                          | `freebox_airmedia_status`, `freebox_airmedia_receivers`                                                                                                                                      | Lecture seule ; pas d'adresse ou de mot de passe.                                  |
| Lire le contrôle parental              | `freebox_parental_config`, `freebox_parental_filters`                                                                                                                                        | Configuration et filtres bornés ; identités réseau masquées.                       |
| Lire les espaces visibles              | `freebox_nas_visibility`, `freebox_shares_visibility`                                                                                                                                        | Lecture seule ; identifiants hôtes hachés et noms d'interface assainis.            |

## Configuration utile

- `FREEBOX_SERVER_ADDRESS` : adresse absolue de l'API, par défaut `https://mafreebox.freebox.fr/`.
- `FREEBOX_ALLOW_PAIRING_INITIATION` : doit valoir exactement `1`, `true` ou `TRUE` pour démarrer un appairage.
- `FREEBOX_PAIRING_POLL_INTERVAL_SECONDS` : intervalle d'attente par défaut (défaut `2`, > 0).
- `FREEBOX_PAIRING_WAIT_TIMEOUT_SECONDS` : délai global par défaut (défaut `600`, > 0).
- `FREEBOX_CREDENTIAL_FILE` et `FREEBOX_CREDENTIAL_PASSPHRASE_FILE` : stockage chiffré optionnel pour les conteneurs
  et environnements headless.

## Comportement au démarrage

Quand `FREEBOX_AUTHENTICATE_ON_STARTUP=true` (par défaut), le serveur tente de réutiliser les credentials persistés au
démarrage.
Si absents et appairage interdit, il remonte un état exploitable sans forcer l’appairage.

## Règles de sécurité

- Ne jamais fournir de token dans le chat, les arguments, ou des variables d’environnement visibles.
- L’appairage exige toujours une validation physique sur l’écran Freebox.
- Les sorties sont sanitizées et bornées.
