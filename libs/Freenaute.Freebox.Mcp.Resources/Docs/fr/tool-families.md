# Guide des familles d'outils MCP Freebox (français)

Utilisez ce guide pour les familles opérationnelles en lecture seule qui complètent l'appairage et le statut en direct.
Le serveur reste local uniquement et exposé uniquement en stdio. Préférez des outils ciblés et des limites faibles :
un résultat borné n'est pas un inventaire complet. Référence officielle : https://dev.freebox.fr/sdk/os/

## Famille Téléchargements

Outils :

- `freebox_downloads_list`
- `freebox_download_status`
- `freebox_download_stats`
- `freebox_download_configuration`
- `freebox_download_files`
- `freebox_download_log`
- `freebox_download_pieces`

Routage : sélectionnez d'abord une tâche avec `list` ou `status`, puis lisez fichiers/journal/pièces avec l'`id`.
Les noms et statuts sont assainis. Les chemins, hachages de fichiers, identifiants et URL de prévisualisation ne sont
pas exposés.

## Famille Appels

Outils :

- `freebox_calls_log`
- `freebox_calls_account`
- `freebox_calls_mark_all_read`
- `freebox_calls_clear_log`

Routage : utilisez le journal pour l'état/date des appels, pas pour reconstruire une identité. Les identifiants
sont hachés et les numéros ne sont pas renvoyés. Toute action de mutation doit être explicitement confirmée.

## Famille Contacts

Outils :

- `freebox_contacts_list`
- `freebox_contacts_search`

Routage : utilisez `search` pour un nom précis, `list` pour un aperçu filtré. Les sorties restent bornées et
n'exposent pas les charges privées brutes.

## Famille Caméras

Outils :

- `freebox_cameras_overview`
- `freebox_home_status`

Routage : caméras pour l'inventaire/état, home status pour la veille et l'état de mise à jour.
Ces outils restent en lecture seule.

## Famille Notifications

Outils :

- `freebox_status_feed`
- `freebox_notifications_feed`

Routage : `status_feed` pour un résumé de santé, `notifications_feed` pour les alertes actionnables.
Les entrées sont des instantanés observés, pas un historique d'événements.

## Famille AirMedia

Outils :

- `freebox_airmedia_status`
- `freebox_airmedia_receivers`

Routage : `status` pour l'état global activé/configuré, `receivers` pour l'inventaire.
Les mots de passe ne sont jamais exposés.

## Famille NAS et partages

Outils :

- `freebox_nas_visibility`
- `freebox_shares_visibility`

Routage : NAS pour l'état stockage/disques, partages pour la visibilité des interfaces LAN.
Les identifiants d'hôtes et chemins de partage sont exclus.

## Famille Contrôle parental

Outils :

- `freebox_parental_config`
- `freebox_parental_filters`

Routage : config pour le mode global, filters pour un résumé d'état borné. Les MAC/IP/noms d'hôtes
et cibles brutes ne sont pas exposés.

## Règles d'usage MCP

- Local-only : ne jamais exposer ce serveur via un endpoint public.
- Authentification : ne jamais demander de token en chat.
- Assainissement : conserver uniquement des champs bornés et non sensibles.
- Identifiants : traiter les SHA-256 comme opaques.
- Limites : une limite de sortie n'implique pas l'absence d'autres éléments côté Freebox.
