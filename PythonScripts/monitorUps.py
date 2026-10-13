# sudo apt install nut
# Configurer le ups dans /etc/nut/ups.conf (voir le Readme), puis valider avec
# upsc <nom-ups>@localhost

# variables utilisées
# battery.charge   charge de la batterie en %
# battery.runtime  autonomie restante en secondes
# ups.status       OL = sur le secteur, OB = sur la batterie (panne), LB = batterie faible

import subprocess
import sys

def lire_ups(ups):
  sortie = subprocess.run(["upsc", ups], capture_output=True, text=True, timeout=10, check=True).stdout
  infos = {}
  for ligne in sortie.splitlines():
    cle, _, valeur = ligne.partition(":")
    infos[cle.strip()] = valeur.strip()
  return infos

if len(sys.argv) == 1:
  print("usage: monitorUps.py <nom-ups>@localhost [<api-domain> <authType> <capteur-charge-id> <capteur-alimentation-id> [<capteur-autonomie-id>]]")
  exit(1)

infos = lire_ups(sys.argv[1])

statut = infos.get("ups.status", "")
charge = float(infos["battery.charge"])
# 1 = alimenté par le secteur, 0 = panne en cours (sur la batterie)
alimentation = 0 if "OB" in statut.split() else 1
autonomie = infos.get("battery.runtime")

print("charge", charge, "statut", statut, "alimentation", alimentation, "autonomie", autonomie)

if len(sys.argv) < 6:
  exit()

from proxy.erabliere_api_proxy import ErabliereApiProxy

proxy = ErabliereApiProxy(sys.argv[2], sys.argv[3])

print(proxy.envoyer_donnee_capteur_v2(sys.argv[4], charge))
print(proxy.envoyer_donnee_capteur_v2(sys.argv[5], alimentation, statut))

if len(sys.argv) > 6 and autonomie is not None:
  # autonomie en minutes
  print(proxy.envoyer_donnee_capteur_v2(sys.argv[6], round(float(autonomie) / 60, 1)))
