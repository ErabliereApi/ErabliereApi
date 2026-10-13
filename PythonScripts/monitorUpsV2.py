# Version 2 de monitorUps.py. Le même script est lancé de deux façons :
#  - par upsmon (NOTIFYCMD) au passage sur batterie et au retour du secteur, pour envoyer le changement tout de suite
#  - par cron à chaque minute, pour la charge, l'autonomie et les envois en attente
#
# Si l'envoi échoue (le routeur tombe souvent avec le courant), la donnée est gardée
# avec son heure d'origine et renvoyée à la prochaine exécution.
#
# L'alimentation à 0 est envoyée une seule fois par panne, l'api déclenche l'alerte à chaque donnée reçue sous le seuil.
#
# Configuration de NUT et de upsmon : voir le Readme

import argparse
import fcntl
import json
import os
import pwd
import requests
import subprocess
import time
from datetime import datetime, timezone

# nombre maximal de données gardées en attente, les plus vieilles sont retirées en premier
MAX_FILE = 2000
# nombre d'envois en échec tolérés par exécution
MAX_ECHECS = 3
# secondes d'attente de la réponse de l'api, elle répond lentement quand la donnée déclenche une alerte
DELAI_LECTURE = 30

utilisateur = pwd.getpwuid(os.getuid())
# upsmon lance NOTIFYCMD sans les variables d'environnement de l'utilisateur, le proxy a besoin de USER
os.environ["USER"] = utilisateur.pw_name
dossier = os.path.join(utilisateur.pw_dir, ".erabliereapi")

def lire_ups(ups):
  sortie = subprocess.run(["upsc", ups], capture_output=True, text=True, timeout=10, check=True).stdout
  infos = {}
  for ligne in sortie.splitlines():
    cle, _, valeur = ligne.partition(":")
    infos[cle.strip()] = valeur.strip()
  return infos

def vider_file(proxy, file):
  echecs = 0
  while len(file) > 0:
    d = file[0]
    debut = time.monotonic()
    try:
      r = proxy.envoyer_donnee_capteur_v2(d["idCapteur"], d["V"], d.get("text"), d["D"])
    except Exception as e:
      print(e)
      if isinstance(e, requests.exceptions.ReadTimeout) and time.monotonic() - debut >= DELAI_LECTURE:
        # l'api a reçu la donnée mais tarde à répondre, par exemple pendant l'envoi d'une alerte.
        # La renvoyer créerait un doublon et une autre alerte. Un délai plus court vient de la
        # négociation TLS, la donnée n'est pas partie.
        file.pop(0)
        continue
      # un réseau instable échoue par intermittence, on réessaie avant d'abandonner jusqu'à la prochaine exécution
      echecs += 1
      if echecs >= MAX_ECHECS:
        return
      continue
    if r.status_code == 400 or r.status_code == 404:
      # la donnée ne sera jamais acceptée, on ne bloque pas la file avec
      print("donnée rejetée", r.status_code, r.text, d)
    elif r.status_code >= 300:
      print(r.status_code, r.text)
      return
    file.pop(0)

parser = argparse.ArgumentParser()
parser.add_argument("--config", default=os.path.join(dossier, "monitorUps.json"))
parser.add_argument("--lecture", action="store_true", help="afficher l'état du ups sans rien envoyer")
parser.add_argument("message", nargs="?", help="message passé par upsmon, non utilisé")
args = parser.parse_args()

with open(args.config) as f:
  config = json.load(f)

infos = lire_ups(config.get("ups", "ups@localhost"))

statut = infos.get("ups.status", "")
charge = float(infos["battery.charge"])
# 1 = alimenté par le secteur, 0 = panne en cours (sur la batterie)
alimentation = 0 if "OB" in statut.split() else 1
autonomie = infos.get("battery.runtime")
# ONLINE, ONBATT ou LOWBATT quand le script est lancé par upsmon
evenement = os.environ.get("NOTIFYTYPE")

print("charge", charge, "statut", statut, "alimentation", alimentation, "autonomie", autonomie, "evenement", evenement)

if args.lecture:
  exit()

from proxy.erabliere_api_proxy import ErabliereApiProxy

proxy = ErabliereApiProxy(config["url"], config.get("authType"))
# (connexion, lecture)
proxy.timeout = (5, DELAI_LECTURE)

maintenant = datetime.now(timezone.utc).isoformat()

# le verrou empêche cron et upsmon de modifier la file en même temps
with open(os.path.join(dossier, "monitorUps.etat.json"), "a+") as f:
  fcntl.flock(f, fcntl.LOCK_EX)
  f.seek(0)
  contenu = f.read()
  etat = json.loads(contenu) if contenu else {"alimentation": None, "file": []}
  file = etat["file"]

  if alimentation == 1 or etat["alimentation"] != alimentation:
    texte = statut if evenement is None else statut + " " + evenement
    file.append({"idCapteur": config["capteurAlimentation"], "V": alimentation, "text": texte[:50], "D": maintenant})
    etat["alimentation"] = alimentation

  file.append({"idCapteur": config["capteurCharge"], "V": charge, "D": maintenant})

  if config.get("capteurAutonomie") and autonomie is not None:
    # autonomie en minutes
    file.append({"idCapteur": config["capteurAutonomie"], "V": round(float(autonomie) / 60, 1), "D": maintenant})

  del file[:-MAX_FILE]

  try:
    vider_file(proxy, file)
  finally:
    f.seek(0)
    f.truncate()
    json.dump(etat, f)

print("en attente", len(file))
