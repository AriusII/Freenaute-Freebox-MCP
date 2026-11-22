# Authentification et appairage Freebox (FR)

## Modèle officiel

Le modèle Freebox OS repose sur:

1. Autorisation d’application (`/login/authorize`) pour obtenir un `app_token`
2. Suivi d’état (`/login/authorize/{track_id}`)
3. Ouverture de session (`/login/session`) via `HMAC-SHA1(app_token, challenge)`

Référence: https://dev.freebox.fr/sdk/os/login/

## Correspondance MCP

- `freebox_pairing_status` : lit l’état du coffre, sans afficher de jeton.
- `freebox_pairing_initiate` : démarre l’autorisation uniquement si
  `FREEBOX_ALLOW_PAIRING_INITIATION` vaut exactement `1`, `true` ou `TRUE`.
- `freebox_pairing_wait` : attend l’approbation physique. Il accepte `pollIntervalSeconds` et `timeoutSeconds`,
  qui doivent être strictement positifs ; il peut être annulé puis repris.

L’approbation physique sur l’écran LCD de la Freebox reste obligatoire : aucun outil ne peut contourner cette étape.

## Séquence sûre

1. Appelez `freebox_pairing_status` avant toute opération.
2. Si l'état est `Missing`, indiquez que l'appairage exige l'opt-in hôte
   `FREEBOX_ALLOW_PAIRING_INITIATION` ainsi qu'une confirmation humaine.
3. Appelez `freebox_pairing_initiate` uniquement si cette variable vaut exactement `1`, `true` ou `TRUE`.
4. Faites approuver l'application sur l'écran LCD. Le client MCP ne peut pas approuver à distance.
5. Appelez `freebox_pairing_wait` avec des entiers positifs facultatifs `pollIntervalSeconds` et
   `timeoutSeconds`; le retard revient aux valeurs de l'hôte.
6. Arrêtez-vous sur `Approved`, `Denied` ou `Expired`.

Ne demandez jamais de jeton applicatif dans le chat ni dans les arguments. Ne placez aucun jeton dans les logs,
les fichiers de configuration ou les variables d'environnement. Le serveur n'expose pas de commande CLI d'import :
le cycle d'authentification passe uniquement par les outils MCP d'appairage.

## Persistance multi-OS

- Windows : Credential Manager.
- Linux : Secret Service/libsecret via `secret-tool` lorsqu’il est disponible.
- Conteneurs ou hôtes headless : stockage fichier chiffré AES-256-GCM uniquement si
  `FREEBOX_CREDENTIAL_FILE` et `FREEBOX_CREDENTIAL_PASSPHRASE_FILE` sont configurés.
- Autres plateformes ou stockage indisponible : comportement fail-closed, sans stockage ni retour arrière en clair.

Le jeton de session reste en mémoire. Seul le jeton applicatif durable peut être persisté dans le stockage sécurisé.

## Authentification au démarrage

Quand `FREEBOX_AUTHENTICATE_ON_STARTUP=true` (défaut), le serveur tente de réutiliser les identifiants persistés
au démarrage. Une absence d'identifiants est signalée sans démarrer automatiquement l'appairage. Avec
`false`, aucune réauthentification automatique n'est tentée au démarrage.
