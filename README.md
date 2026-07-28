# ErabliereApi
Solution de monitoring pour érablière. Contient un REST API ainsi qu'un application web pour la gestion des données et d'autre script permettant de connecter divers appareils.

L'application est accessible à l'url: https://erabliereapi.freddycoder.com/ une authentification est nécessare pour la majorité des fonctionnalités. Pour obtenir un compte, veuillez accéder au instruction dans la section À propos.

Un démo est accessible à l'url: https://erabliereapi-demo.azurewebsites.net/ qui ne nécessite pas d'authentification.

Page de présentation: https://erabliereapi.ca

## But
Le but de ce projet est d'analyser, lever des alertes et automatiser certain mecanisme. Basé sur les données receuillis et de façon centralisé.

L'information pourrait aussi bien venir d'appeil ayant la capacité de faire des requêtes http ou d'interaction humaine.

## Suivit du projet

Le suivit du projet est effectué dans AzureDevOps : https://dev.azure.com/freddycoder/ErabliereAPI

## Structure

### Diagramme de haut niveau de la solution déployé dans AKS

![Architecture Diagram](https://github.com/freddycoder/ErabliereApi/blob/master/Diagrams/ErabliereApi.drawio.png?raw=true)

### Dossier du repository
- ErabliereAPI : Projet du web API
- ErabliereIU : Application angular pour l'affichage des données
- ErabliereModel : Projet du modèles de données
- ErabliereApi.Proxy : Proxy pour le web API disponible soous forme de nuget
- ErabliereApi.Mcp : Serveur MCP exposant l'API aux assistants IA (voir la section Serveur MCP)
- Infrastructure : Fichier relié à la configuration de l'infrastructure	kubernetes ou autres
- PythonScripts : Script python pour alimenter l'API

## Modèles de données
Les informations enregistré peuvent être :

- Érablière. Noeud racine de la structure de donnée
- Capteurs. Représente un capteur
- DonneeCapteur. Une donnée d'une capteur
- Temperature, Vaccium, Niveau du bassin
- Les dompeux
- Informations sur les barils
- Alertes
- Rapports
- Notes
- Documentations
- Conversations avec ErabliereAI
- 

## Utilisation

Ce projet est utilisable de différente manière :
1. Rouler directement dans un environnement de développement.
2. Déployer sur un PC avec le .net 9 runtime d'installé
2. Utilisation avec Docker
3. Utilisation avec Kubernetes

Une image docker est disponible sur dockerhub:

```
docker pull erabliereapi/erabliereapi
docker run -d -p 9001:80 erabliereapi/erabliereapi
```

Une librairie proxy est disponible sur nuget.org: ```ErabliereAPI.Proxy```.

## Serveur MCP

Le projet `ErabliereApi.Mcp` est un serveur [Model Context Protocol](https://modelcontextprotocol.io)
qui expose les données d'une érablière à un assistant IA (Claude Code, Claude Desktop, ...) sous
forme d'outils en lecture seule : liste des érablières, capteurs et leurs données résumées, alertes,
dompeux, notes, rapports, barils et horaire.

Deux façons de l'utiliser :

- **Serveur hébergé, en HTTP.** Rien à installer, l'assistant se connecte à l'url du serveur avec
  votre clé d'api :

  ```powershell
  claude mcp add --transport http erabliereapi https://mcp.erabliereapi.freddycoder.com/mcp --header "X-ErabliereApi-ApiKey: <votre-clé-d-api>"
  ```

- **Serveur local, en stdio.** L'assistant démarre lui-même le serveur comme processus enfant. Voir
  le [Readme du projet](ErabliereApi.Mcp/Readme.md) pour la configuration de `.mcp.json` et de
  Claude Desktop.

La clé d'api se crée dans l'application web sous *Profil -> Clés d'api*. Restreignez-la au verbe
`GET` : le serveur MCP n'écrit jamais.

**Forfait requis.** Le serveur **hébergé** peut être restreint aux forfaits d'abonnement qui
incluent l'accès MCP ; sur `erabliereapi.freddycoder.com`, c'est le forfait `base`. L'outil
`get_my_plan` répond quel forfait est associé à votre clé et ce qu'il donne accès. Un appel refusé
reçoit un message expliquant le forfait à souscrire. Le serveur **local** (stdio), que vous hébergez
vous-même avec votre propre clé, n'est jamais restreint.

La correspondance forfait -> accès est configurable (`Mcp:PlanGating` dans `appsettings.json`) et le
gating est désactivé par défaut. Détails dans le [Readme du projet](ErabliereApi.Mcp/Readme.md).

## ErabliereAI répond avec vos données

La conversation ErabliereAI de l'application web utilise **le même jeu d'outils** que le serveur MCP.
Une question comme « Quelle était la température de mon érablière hier ? » n'est plus répondue de
mémoire : l'assistant appelle `get_donnees_capteur`, lit les vrais relevés et cite les valeurs. Les
réponses construites ainsi portent une pastille *Données réelles*, et le détail des consultations est
repliable sous la réponse.

Douze outils sont offerts à la conversation, tous en lecture seule : `list_erablieres`,
`get_erabliere`, `list_capteurs`, `get_donnees_capteur`, `get_alertes`, `get_alertes_capteur`,
`get_notes`, `get_barils`, `get_dompeux`, `get_horaire`, `list_rapports`, `get_rapport`. Aucun outil
d'écriture ou de suppression n'est exposé, et `get_my_plan` est écarté (il répond sur le client MCP,
pas sur l'érablière).

**L'assistant ne voit que vos données.** Il ne détient aucune identité : chaque appel d'outil
rappelle ErabliereAPI en transportant les identifiants de la requête en cours (jeton ou clé d'api),
et repasse donc par l'authentification, l'autorisation et les filtres de propriété habituels. Aucune
clé de service n'existe, donc aucun chemin ne permet à la conversation de lire une érablière que son
utilisateur ne pourrait pas lire lui-même. Voir la
[note d'architecture](Diagrams/ErabliereAI-Outils-MCP.md).

**Forfait requis.** L'accès aux outils depuis la conversation réutilise la configuration
`Mcp:PlanGating` : la capacité `mcp` ouvre les deux. Sans elle, la conversation fonctionne comme
avant — le modèle répond de ses propres connaissances — et l'interface affiche une invitation
discrète à s'abonner.

### Configuration

| Clé | Défaut | Rôle |
|---|---|---|
| `ErabliereAI:Tools:Enabled` | `true` | Interrupteur général. À `false`, aucun outil n'est déclaré. |
| `ErabliereAI:Tools:MaxRounds` | `5` | Nombre maximal d'allers-retours modèle -> outils -> modèle par question. |
| `ErabliereAI:Tools:ToolTimeout` | `00:00:20` | Délai accordé à un appel d'outil avant abandon. |
| `ErabliereAI:Tools:TokenBudget` | `12000` | Plafond de jetons que les résultats d'outils peuvent occuper. |
| `ErabliereAI:Tools:ExcludedTools` | `["get_my_plan"]` | Outils volontairement non offerts. |
| `ErabliereAI:Tools:ApiBaseUrl` | *(vide)* | Adresse interne de l'API. Vide, l'adresse de la requête servie est utilisée. |
| `Mcp:PlanGating` | *(voir plus haut)* | Forfaits ouvrant la capacité `mcp`, partagés avec le serveur MCP. |

Chaque limite se termine de la même façon : un dernier tour **sans outil**, où le modèle répond avec
ce qu'il a recueilli. Une limite atteinte ne produit donc jamais d'erreur pour l'utilisateur.

Le fournisseur Gemini reste sans outil tant que `GoogleGenAIEnableToolCalling` n'est pas à `true` :
la traduction des messages de résultat d'outil n'y est pas implémentée, et la conversation dégrade
proprement plutôt que d'envoyer au modèle une conversation subtilement fausse.

## Persistance des données

Deux mode sont possible. 

1. Mode en mémoire (aucune persistance, avec swagger il est possible de télécharger les données sous format json et de les stocker manuellement)
2. Sql avec EntityFramework ( Voir le readme dans https://github.com/freddycoder/ErabliereApi/tree/master/ErabliereApi/Depot/Sql )

## Documentation additionnelle

### Lancer les scripts de génération de donnée python pour le développement avec cron

La cronjob suivante va lancer le script de génération de donnée pour 2 érablière en utilisant l'api de l'adresse spécifié

```
crontab -e
*/1 * * * * python3 /home/ubuntu/erabliereapi/GenerateurDonneePython/donnees.py 2 http://192.168.0.103:5000
```

### Extraire les logs sauf pour certain paramètre

```bash
kubectl logs --since=24h pods/my-nginx-deployment-5977f4fdff-p7t5r | grep erabliere | grep -i -v 'param1|param2'
```

### Compiler l'image docker

À la racine du repo, executer ```docker build -t erabliereapi:local .```

### Déployer la solution avec docker desktop

Prerequis: Powershell core : https://learn.microsoft.com/fr-fr/powershell/scripting/install/installing-powershell-on-windows?view=powershell-7.3#installing-the-msi-package

Avec powershell core en tant qu'administrateur executer le script ```.\deploiement-local-aad.ps1``` puis ensuite ```docker compose up -d```. Pour mettre à jour un déploiement docker compose, executez ```docker compose up -d --force-recreate```. Si vous voulez télécharger les images plus récente, lancer ```docker compose pull``` avant d'executer la commande --force-recreate.

### Intégration Stripe

À partir de la version 3 (v3-dev ou latest), l'api offre une intégration avec Stripe. Pour utiliser Stripe, il faut initialiser quelque variable d'environnement :

```
  "Stripe.ApiKey": "sk_test_...",
  "Stripe.SuccessUrl": "https://...",
  "Stripe.CancelUrl": "https://...",
  "Stripe.BasePlanPriceId": "price_...",
  "Stripe.AbonnementMensuelPriceId": "price_...",
  "Stripe.AbonnementAnnuelPriceId": "price_...",
  "Stripe.WebhookSecret": "secure-...",
  "Stripe.WebhookSiginSecret": "whsec_...",
```

`Stripe.BasePlanPriceId` est le prix facturé à l'utilisation des clés d'API. Les abonnements
de compte utilisateur utilisent plutôt deux prix récurrents à fréquence fixe :
`Stripe.AbonnementMensuelPriceId` (16 $/mois) et `Stripe.AbonnementAnnuelPriceId` (166 $/an).
Le webhook distingue les deux produits selon le price id de la souscription Stripe.

Et démarrer le stripe CLI dans un terminal :

> Sur windows, il faut télécharger l'executable et s'assurer qu'il soit disponible dans le PATH.
> Télécharger le CLI de stripe : https://github.com/stripe/stripe-cli/releases/

La première fois, il faut se connecter avec ```stripe login```.
```
stripe login
stripe listen --forward-to localhost:5000/Checkout/Webhook
dotnet user-secrets set Stripe.WebhookSiginSecret <webhook-signing-secret-from-previous-command>
```

> Plus d'information: https://stripe.com/docs/webhooks/test

Notez que les configuration courriels doivent être configuré pour que les utilisateurs puissent recevoir leur clé d'api. Les configurations courriel sont documenté ici: https://github.com/freddycoder/ErabliereApi/tree/master/Infrastructure#fonctionnalit%C3%A9-dalerte

### Déployer l'interface sur une installation apache2 d'un raspberry pi

> Image utilisé Ubuntu server 20.04 32 bits

```bash
cd ErabliereIU
npm install
ng build --prod
sudo rm -r /var/www/html/*
sudo cp -r dist/ErabliereIU/* /var/www/html/
```

### Documentation sur les configuration réseau ubuntu server

https://netplan.io/examples/

### Configuration office365

https://www.powershellgallery.com/packages/ExchangeOnlineManagement/2.0.4

https://docs.microsoft.com/en-us/powershell/exchange/connect-to-exchange-online-powershell?view=exchange-ps

### Utiliser ubuntu server derrière une connexion internet limité

> Utiliser seulement derrière des connexion internet limité

Ubuntu effectue des mises à jour de sécurité en arrière plan et peut avoir un impacte sur le nombre de donnée échangé par le système d'expoitation et l'ordinateur.

```
sudo systemctl disable apt-daily.timer
sudo systemctl disable apt-daily-upgrade.timer
sudo systemctl disable dpkg-db-backup.timer
sudo systemctl disable man-db.timer
sudo systemctl disable update-notifier-download.service
```

> Pour arrêter le service immédiatement sans faire de redémarrage, aussi effectuer ```sudo systemctl stop <service-name>```.

Pour lister les processus
```
systemctl list-timers
```

Exemple de output:
```
NEXT                            LEFT LAST                              PASSED UNIT                           ACTIVATES
Sun 2025-03-09 09:10:00 EDT 2min 51s Sun 2025-03-09 09:00:01 EDT     7min ago sysstat-collect.timer          sysstat-collect.service
Sun 2025-03-09 09:44:13 EDT    37min Sun 2025-03-09 08:53:02 EDT    14min ago fwupd-refresh.timer            fwupd-refresh.service
Sun 2025-03-09 22:07:07 EDT      12h Sun 2025-03-09 07:13:02 EDT 1h 54min ago motd-news.timer                motd-news.service
Sun 2025-03-09 22:20:01 EDT      13h Sat 2025-03-08 21:20:01 EST      10h ago update-notifier-download.timer update-notifier-download.service
Sun 2025-03-09 22:30:10 EDT      13h Sat 2025-03-08 21:30:10 EST      10h ago systemd-tmpfiles-clean.timer   systemd-tmpfiles-clean.service
Mon 2025-03-10 00:00:00 EDT      14h Sun 2025-03-09 00:00:01 EST       8h ago dpkg-db-backup.timer           dpkg-db-backup.service
Mon 2025-03-10 00:00:00 EDT      14h Sun 2025-03-09 00:00:01 EST       8h ago logrotate.timer                logrotate.service
Mon 2025-03-10 00:07:00 EDT      14h Sun 2025-03-09 00:07:02 EST       8h ago sysstat-summary.timer          sysstat-summary.service
Mon 2025-03-10 00:40:03 EDT      15h Mon 2025-03-03 01:11:50 EST   6 days ago fstrim.timer                   fstrim.service
Mon 2025-03-10 03:52:07 EDT      18h Sun 2025-03-09 08:46:50 EDT    20min ago man-db.timer                   man-db.service
Thu 2025-03-13 16:33:12 EDT   4 days Wed 2025-03-05 21:01:50 EST   3 days ago update-notifier-motd.timer     update-notifier-motd.service
Sun 2025-03-16 03:10:39 EDT   6 days Sun 2025-03-09 03:10:20 EDT 5h 56min ago e2scrub_all.timer              e2scrub_all.service
```
