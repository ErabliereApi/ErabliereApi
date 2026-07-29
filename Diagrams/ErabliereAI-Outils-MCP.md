# ErabliereAI et les outils MCP

Comment la conversation ErabliereAI répond avec les données réelles de l'utilisateur, et pourquoi
elle ne peut lire que les siennes.

## Le choix : les outils s'exécutent dans l'API

Deux intégrations étaient possibles.

**Retenue — en processus.** `ErabliereApi` référence `ErabliereApi.Mcp.Tools` — la bibliothèque
qui porte le jeu d'outils, et non l'exécutable du serveur — et transforme les méthodes
`[McpServerTool]` en définitions d'outils pour le modèle. Les corps d'outils, eux, sont inchangés :
ils lisent ErabliereAPI par `IErabliereAPIProxy`, dont l'instance enregistrée par l'API pointe vers
l'API elle-même et **transporte les identifiants de la requête en cours**.

**Écartée — client MCP en HTTP.** Se connecter au serveur MCP déployé obligerait l'API à détenir ou
à émettre une clé d'api au nom de l'utilisateur, ce qui créerait une seconde surface d'autorisation
en dehors du pipeline de l'API, précisément pour les données que ce pipeline existe pour protéger.

Aucune description d'outil, aucun schéma JSON et aucun filtre de propriété n'est donc écrit deux
fois : le serveur MCP et la conversation offrent le même jeu d'outils, et la conversation autorise
exactement comme le reste de l'API.

## L'autorisation

```
┌──────────┐  POST /ErabliereAI/Prompt          ┌───────────────────────────────────────────┐
│ Fureteur │  Authorization: Bearer …           │            ErabliereAPI                   │
│    ou    │  ou X-ErabliereApi-ApiKey: …       │                                           │
│  client  ├───────────────────────────────────►│  ErabliereAIController                    │
│  d'api   │                                    │      │                                    │
└──────────┘                                    │      ▼                                    │
                                                │  ConversationAIService ──► IAIService ──► LLM
                                                │      │                                    │
                                                │      ▼                                    │
                                                │  McpErabliereAiToolset                    │
                                                │      │  (ErabliereApi.Mcp.Tools)          │
                                                │      ▼                                    │
                                                │  IErabliereAPIProxy                       │
                                                │      │                                    │
                                                │      │ CallerCredentialsHandler recopie   │
                                                │      │ les en-têtes de la requête servie  │
                                                │      ▼                                    │
                                                │  GET /Erablieres, /Capteurs, …            │
                                                │  ► authentification                       │
                                                │  ► autorisation, ValiderOwnership         │
                                                │  ► filtres CustomerErabliere              │
                                                └───────────────────────────────────────────┘
```

Le point qui fait tenir l'ensemble : **l'IA ne détient aucun identifiant**. Pas de compte de service,
pas de clé d'api, pas de jeton élevé. `CallerCredentialsHandler` recopie l'en-tête `Authorization` ou
`X-ErabliereApi-ApiKey` de la requête servie, et refuse d'émettre l'appel lorsqu'il n'y a rien à
recopier alors que l'authentification est activée. Il n'existe donc pas de chemin par lequel la
conversation pourrait élargir la portée de son utilisateur : il n'y a rien avec quoi l'élargir.

Corollaire utile : l'identifiant d'érablière que le client envoie avec le prompt n'est **pas** une
autorisation. Il est écrit dans la phrase système pour que le modèle cesse de deviner de quelle
érablière on parle ; un identifiant qui n'appartient pas à l'appelant mène à un appel d'outil refusé
par l'API, exactement comme s'il l'avait tapé lui-même dans le fureteur.

Les requêtes que l'API s'adresse à elle-même portent l'en-tête `X-ErabliereApi-Loopback` avec un
jeton tiré au démarrage et jamais publié (`LoopbackRequestMarker`). Il ne sert qu'à empêcher qu'un
mécanisme qui sérialise les requêtes fasse attendre l'appel imbriqué derrière celui qui l'attend.

## La boucle

```
Client        ErabliereAIController   ConversationAIService      Toolset          IAIService      API (elle-même)
  │ POST /Prompt        │                     │                     │                 │                │
  ├────────────────────►│                     │                     │                 │                │
  │                     ├─ SendPromptAsync ──►│                     │                 │                │
  │                     │                     ├─ capacités du forfait (Mcp:PlanGating) │                │
  │                     │                     ├─ GetChatTools() ───►│                 │                │
  │                     │                     │◄─ 12 ChatTool ──────┤                 │                │
  │ GET /Prompt/Status  │                     │                                       │                │
  │◄─ « Consultation… » ┤◄═ IToolActivityTracker ═╡  TOUR 1..5                         │                │
  │  (sondage, 1 s)     │                     ├─ CompleteChatAsync(messages, outils) ─►│                │
  │                     │                     │◄─ finish=ToolCalls, [get_donnees_capteur]                │
  │                     │                     ├─ InvokeAsync (délai par outil) ───────►│                │
  │                     │                     │                     ├─ proxy + identifiants de l'appelant ►│
  │                     │                     │                     │◄─ {summary, data, truncated} ───────┤
  │                     │                     ├─ ajoute Assistant(toolCalls) + Tool(résultat)             │
  │                     │                     ├─ tours ou budget épuisés ? le dernier appel n'offre AUCUN outil
  │                     │                     ├─ CompleteChatAsync(messages) ─────────►│                │
  │                     │                     │◄─ finish=Stop, texte ──────────────────┤                │
  │                     │                     ├─ persiste : question, AppelOutil, ResultatOutil, réponse │
  │◄─ PostPromptResponse┤◄────────────────────┤                                        │                │
```

### Les trois limites

| Limite | Défaut | Ce qui se passe quand elle est atteinte |
|---|---|---|
| Tours d'outils | 5 | Un dernier tour sans outil, avec la consigne de répondre avec ce qui a été recueilli. |
| Délai par outil | 20 s | L'appel est abandonné et le modèle reçoit un résultat d'erreur expliquant comment se reprendre. |
| Budget de jetons | 12 000 | La boucle s'arrête après le tour courant, puis dernier tour sans outil. |

Aucune de ces limites ne produit d'erreur pour l'utilisateur : elles produisent une réponse moins
complète, et le modèle est invité à dire ce qu'il n'a pas pu vérifier.

## La persistance

Un échange enregistre, dans l'ordre de `CreatedAt` :

| `MessageType` | Contenu | Affichage |
|---|---|---|
| `Texte` | La question de l'utilisateur | Tour de parole |
| `AppelOutil` | Les arguments JSON produits par le modèle | Trace repliable |
| `ResultatOutil` | L'enveloppe `{summary, data, truncated}` ou `{error}` | Trace repliable |
| `Texte` | La réponse, avec `UsedLiveData` | Tour de parole + pastille |

Les messages d'outil sont conservés pour l'interface, **jamais rejoués** dans l'historique envoyé au
modèle : la réponse qui les suit porte déjà ce qu'ils disaient, et un message d'assistant dont les
appels d'outils ne sont pas immédiatement suivis de leurs résultats est refusé par les API de
complétion.

`UsedLiveData` n'est vrai que si au moins un outil a réellement retourné des données. Un tour d'appels
tous en échec ne décore pas la réponse d'une pastille affirmant qu'elle repose sur des relevés réels.

## Le forfait

La conversation lit la même section de configuration que le serveur MCP, `Mcp:PlanGating`, et la
même capacité `mcp`. Le forfait courant vient d'`IAbonnementService`, l'unique autorité que
`ValiderAbonnementAttribute` et `GET /api/Abonnements/Courant` utilisent déjà — sans appel HTTP et
sans seconde définition de « qui est sur quel forfait ».

Forfait insuffisant : aucune définition d'outil n'est envoyée, la conversation répond comme avant,
et `GET /ErabliereAI/Capabilities` permet à l'interface d'afficher une invitation discrète à
s'abonner.
