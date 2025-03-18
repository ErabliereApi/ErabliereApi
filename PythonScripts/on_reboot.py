import sys
from proxy.erabliere_api_proxy import ErabliereApiProxy

authType = None

if len(sys.argv) > 2:
  authType = sys.argv[2]

proxy = ErabliereApiProxy(sys.argv[1], authType)

r = proxy.envoyer_donnee_capteur(sys.argv[len(sys.argv)-1], 1)

print(r)