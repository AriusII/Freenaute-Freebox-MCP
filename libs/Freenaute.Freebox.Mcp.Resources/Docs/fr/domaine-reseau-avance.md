# Domaine réseau avancé

Utilisez ce domaine pour les questions réseau détaillées en lecture seule. Le routage et la configuration avancée
n'appartiennent pas aux opérations d'écriture disponibles.

## Outils

- `freebox_connection_status` : état de la liaison, type, support, compteurs et options.
- `freebox_lan_overview` : interfaces LAN bornées et inventaire facultatif des hôtes sous forme d'identifiants hachés.
- `freebox_wifi_status` : résumé borné des points d'accès et stations Wi-Fi.

Activez `includeHosts=true` uniquement si l'inventaire est utile. `interfaceName` doit contenir 1 à 64 caractères,
sans caractère de contrôle.

## Ressource live

`freebox://live/network-advanced` fournit le résumé réseau assaini. En cas d'indisponibilité, elle retourne un
texte de secours borné, sans détails sensibles.
