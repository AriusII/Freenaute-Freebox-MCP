# Domaine fichiers avancé

Le domaine fichiers couvre les téléchargements et la visibilité NAS/partages. Les opérations d'inventaire sont
limitées à la lecture. Toute suppression ou écriture exige un flux de mutation explicite.

## Téléchargements

- `freebox_downloads_list`, `freebox_download_status`, `freebox_download_stats`
- `freebox_download_configuration`
- `freebox_download_files`, `freebox_download_log`, `freebox_download_pieces`

Les sorties sont bornées et assainies. Chemins, identifiants, mots de passe, hachages, URL de prévisualisation
et journaux bruts sont exclus ou tronqués.

## Stockage et partages

- `freebox_nas_visibility`
- `freebox_shares_visibility`

Utilisez un identifiant issu de la liste ou du statut avant de demander le détail.
