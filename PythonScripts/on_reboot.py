import sys
import time
from proxy.erabliere_api_proxy import ErabliereApiProxy

authType = None

if len(sys.argv) > 2:
  authType = sys.argv[2]

proxy = ErabliereApiProxy(sys.argv[1], authType)

tentative = 0
r = None
while tentative < 20:
  try:
    r = proxy.envoyer_donnee_capteur(sys.argv[len(sys.argv)-1], 1)
    if r.status_code != 200 and r.status_code != 201 and r.status_code != 204:
      print(r.status_code)
      print(r.text)
      tentative += 1
    else:
      break
  except Exception as e:
    tentative += 1
    print(e)

  # before retrying, wait 60 seconds
  time.sleep(60)

print(r)