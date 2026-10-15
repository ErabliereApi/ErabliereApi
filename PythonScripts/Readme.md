# PythonScripts

Les scripts python sont installé sur des ordinateurs permettant d'interoger des appareils qui n'ont pas la capacité d'envoyer des requêtes http.

### Installer les dépendances

Sous Linux:
```
cd ErabliereApi/PythonScripts
sudo sh instal.sh
python3 -m venv my-venv
./my-venv/bin/pip3 install --only-binary :all: --require-hashes -r requirements.txt
```

Sous windows:
```
python -m venv my-venv
.\my-venv\Scripts\Activate.ps1
pip install -r requirements.txt
```

### Authentification

Deux mode d'authentification sont supporté (Aucune authentification et AzureAD). La documentation suivante se concentrera sur l'installation des scripts avec AzureAD.

Les configurations d'authentification en mode 'client credentials' devront se trouvé dans

Linux
```
/home/ubuntu/.erabliereapi/auth.config
```

Windows
```
E:\config\python\aad-client-credentials.json
```

Le json de configuration devra ressembler à ceci:

```
[
    {
        "TenantId": "<tenantId>",
        "ClientId": "<clientId>",
        "ClientSecret": "<clientSecret>",
        "Authority": "https://login.microsoftonline.com",
        "Scopes": "api://<api-clientId>/.default"
    }
]
```

## extraireInfoHmi.py

```
python .\extraireInfoHmi.py http://<ip-address-hmi>/1.jpg https://erabliereapi.freddycoder.com <guid-erabliere>
```

Version plus complexe envoyant les données récupéré à deux API une sans vérification ssl et l'autre avec les vérification ssl.

```
python3 /home/ubuntu/erabliereapi/PythonScripts/extraireInfoHmi.py http:/<ip-address-hmi>/1.jpg [noSslVerify]https://192.168.1.2:5001,https://erabliereapi.freddycoder.com <guid-erabliere>
```

## getwather.py

```
python3 getweather.py <acuweather-api-key-file-path> <location> <api-domain> <capteur-guid-id>
```
ex:
```
python3 getweather.py /home/ubuntu/.erabliereapi/acuweather.key 1365711 https://erabliereapi.freddycoder.com 00000000-0000-0000-0000-000000000000
```

## image2textapi.py

```docker
docker run -d -p 39000:5000 erabliereapi/extraireinfohmi:latest flask run --host=0.0.0.0
```

## monitorRaspberry.py

```
python3 monitorRaspberry.py https://erabliereapi.freddycoder.com AzureAD <capteur-id>
```
ou en utilisant un environnement virtuel
```
./my-venv/bin/python3 monitorRaspberry.py https://erabliereapi.freddycoder.com AzureAD <capteur-id>
```

> Afin de récupérer l'id du capteur, il est possible de le récupérer dans la page capteur de l'api. Utiliser le bouton dans la colonne 'id' pour copier l'id du capteur.

## monitorUps.py

Publie l'état d'une batterie de secours (UPS) branchée en USB : charge, alimentation (1 = secteur, 0 = panne en cours) et autonomie en minutes. Utilise [NUT](https://networkupstools.org/).

```bash
sudo apt install nut
```

Dans `/etc/nut/ups.conf`
```
[ups]
  driver = usbhid-ups
  port = auto
```

Dans `/etc/nut/nut.conf`, mettre `MODE=standalone`, puis
```bash
sudo systemctl restart nut-driver.target nut-server
upsc ups@localhost
```

Lecture seulement
```
python3 monitorUps.py ups@localhost
```
Avec envoi des données, le dernier capteur (autonomie) est optionnel
```
python3 monitorUps.py ups@localhost https://erabliereapi.freddycoder.com AzureAD <capteur-charge-id> <capteur-alimentation-id> <capteur-autonomie-id>
```

Crontab, à chaque minute
```bash
* * * * * cd $EAPIPATH && ./my-venv/bin/python3 monitorUps.py ups@localhost https://erabliereapi.freddycoder.com AzureAD <capteur-charge-id> <capteur-alimentation-id>
```

## monitorUpsV2.py

Même mesures que `monitorUps.py`, avec en plus :

- **Réaction à l'événement.** `upsmon` lance le script au passage sur batterie et au retour du secteur, sans attendre la prochaine minute.
- **File d'attente.** Si l'envoi échoue (le routeur tombe souvent avec le courant), la donnée est gardée avec son heure d'origine dans `~/.erabliereapi/monitorUps.etat.json` et renvoyée à la prochaine exécution.
- **Une alerte par panne.** L'alimentation à 0 est envoyée une seule fois par panne, au lieu d'à chaque minute. La charge et l'autonomie continuent d'être envoyées à chaque minute.

Installer et configurer NUT comme pour `monitorUps.py`, puis créer `~/.erabliereapi/monitorUps.json`. `capteurAutonomie` est optionnel.

Le plus simple : dans l'application, page **Appareils** de l'érablière, bouton **Ajouter un kit panne**. L'API crée les trois capteurs, l'alerte de panne (alimentation à 0), l'alerte de silence sur la charge et une clé d'API qui ne peut que faire des `POST` de données vers ces trois capteurs. La page affiche une seule fois une commande à coller sur le kit, qui écrit `monitorUps.json` (avec `"authType": "ApiKey"`) et `auth.<hôte>.config`. Révoquer le kit depuis la même page coupe sa clé ; les capteurs et leur historique restent.

Pour une configuration manuelle :

```json
{
    "ups": "ups@localhost",
    "url": "https://erabliereapi.freddycoder.com",
    "authType": "AzureAD",
    "capteurCharge": "<capteur-charge-id>",
    "capteurAlimentation": "<capteur-alimentation-id>",
    "capteurAutonomie": "<capteur-autonomie-id>"
}
```

Lecture seulement
```
./my-venv/bin/python3 monitorUpsV2.py --lecture
```

Crontab, à chaque minute. Remplace la ligne de `monitorUps.py`.
```bash
* * * * * cd $EAPIPATH && ./my-venv/bin/python3 monitorUpsV2.py
```

### Configurer upsmon

Dans `/etc/nut/upsd.users` (`upsmon master` avec NUT 2.7)
```
[monitor]
  password = <mot-de-passe>
  upsmon primary
```

Dans `/etc/nut/upsmon.conf` (`master` au lieu de `primary` avec NUT 2.7). `RUN_AS_USER` est l'utilisateur qui possède `~/.erabliereapi`, le script a besoin de ses fichiers d'authentification.
```
MONITOR ups@localhost 1 monitor <mot-de-passe> primary
RUN_AS_USER ubuntu
NOTIFYCMD "/home/ubuntu/erabliereapi/PythonScripts/my-venv/bin/python3 /home/ubuntu/erabliereapi/PythonScripts/monitorUpsV2.py"
NOTIFYFLAG ONLINE SYSLOG+EXEC
NOTIFYFLAG ONBATT SYSLOG+EXEC
NOTIFYFLAG LOWBATT SYSLOG+EXEC
```

Pour capter les pannes très courtes, baisser aussi dans `upsmon.conf` l'intervalle auquel upsmon interroge le ups (5 secondes par défaut). Le pilote interroge lui-même le ups aux 2 secondes.
```
POLLFREQ 1
POLLFREQALERT 1
```

L'utilisateur doit pouvoir lire `upsmon.conf`
```bash
sudo usermod -aG nut ubuntu
sudo systemctl restart nut-server nut-monitor
```

Pour valider, débrancher le ups de la prise et suivre `journalctl -u nut-monitor -f`. Avec cette configuration, upsmon arrête aussi l'ordinateur quand la batterie est faible.

### Redémarrage après l'arrêt à batterie faible

Un Raspberry Pi arrêté ne redémarre que si son alimentation est coupée puis rétablie. Si le courant revient pendant l'arrêt, le ups n'a jamais cessé de l'alimenter et le Pi reste éteint.

NUT prévoit ce cas :

1. Quand la batterie est faible, upsmon crée un fichier témoin (`POWERDOWNFLAG`, `/etc/killpower` sous Debian) puis lance l'arrêt.
2. À la toute fin de l'arrêt, le script `nutshutdown` installé par le paquet voit ce témoin et envoie au ups la commande de coupure (`upsdrvctl shutdown`).
3. Le ups coupe sa sortie après un délai, puis la rétablit quand le secteur est présent. Si le courant est déjà revenu, il fait quand même le cycle, et c'est ce qui redémarre le Pi.

À vérifier :

- `POWERDOWNFLAG` est présent dans `/etc/nut/upsmon.conf`.
- Le ups accepte la commande de coupure. Certains modèles d'entrée de gamme l'ignorent.

Pour valider, avec le ups branché sur le secteur, forcer l'arrêt :
```bash
sudo upsmon -c fsd
```
Le Pi s'éteint, le ups coupe sa sortie quelques secondes plus tard, puis le Pi redémarre. S'il reste éteint, le ups ne supporte pas la commande.

Dans ce cas, la solution de repli est de ne pas arrêter le Pi et de mettre la carte SD en lecture seule (overlayfs). Le Pi tombe avec la batterie et repart dès que le courant revient.

## Mise à jour automatique des scripts

Afin de mettre à jour automatiquement les scripts, il est possible de créer un cron job sous linux.

```bash
crontab -e
```

Ajouter la ligne suivante obtenir une mise à jour quotidienne à minuit.

```bash
0 0 * * * cd /<path-to-erabliereapi> && git pull
```

## nmap_scan.sh

```bash
nmap_scan.sh -i <id-erabliere> -n 192.168.1.0/24
```

Crontab
```bash
5 4 * * 7 $EAPIPATH/nmap_scan.sh -i <id-erabliere> -n 192.168.1.0/24
```

Tout les dimanche à 4:05 AM

https://crontab.guru/#5_4_*_*_7