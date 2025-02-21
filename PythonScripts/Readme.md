# PythonScripts

Les scripts python sont installé sur des ordinateurs permettant d'interoger des appareils qui n'ont pas la capacité d'envoyer des requêtes http.

### Installer les dépendances

Sous Linux:
```
cd ErabliereApi/PythonScripts
sudo sh instal.sh
python3 -m venv my-venv
./my-venv/bin/pip3 install -r requirements.txt
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

## Mise à jour automatique des scripts

Afin de mettre à jour automatiquement les scripts, il est possible de créer un cron job sous linux.

```bash
crontab -e
```

Ajouter la ligne suivante obtenir une mise à jour quotidienne à minuit.

```bash
0 0 * * * cd /<path-to-erabliereapi> && git pull
```