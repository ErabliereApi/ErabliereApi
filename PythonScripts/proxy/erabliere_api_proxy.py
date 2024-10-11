import json
import requests
import os
from urllib.parse import urlparse
from auth.getAccessToken import getAccessToken as getAccessTokenIdentity
from auth.getAccessTokenAAD import AzureADAccessTokenProvider

class ErabliereApiProxy:
  def __init__(self, base_url, auth_provider = None, veryfy_ssl = True):
    self.base_url = base_url
    self.auth_provider = auth_provider
    self.aad_token_provider = AzureADAccessTokenProvider()
    self.auth_config = None
    self.verify_ssl = veryfy_ssl
    self.host = urlparse(base_url).netloc
    self.timeout = 5

  path_e = "/erablieres/"

  def get_donnees(self, id_erabliere: str, q: int, o: str):
    data = self.get_request(self.path_e + str(id_erabliere) + "/donnees?q=" + str(q) + "&o=" + o)
    return data.json()

  def envoyer_donnees(self, id_erabliere: str, temperature: int, vaccium: int, niveaubassin: int):
    donnee = {'t': temperature, 'nb': niveaubassin, 'v': vaccium, 'idErabliere': id_erabliere}
    return self.post_request(self.path_e + str(id_erabliere) + "/donnees", donnee)

  def envoyer_dompeux(self, id_erabliere, datedebut, datefin):
    dompeux = {'idErabliere': int(id_erabliere),
               'dd': datedebut,
               'df': datefin}
    return self.post_request(self.path_e + str(id_erabliere) + "/dompeux", dompeux)

  def envoyer_donnee_capteur(self, id_capteur, valeur, text = None):
    donnee = {'V': valeur, 'idCapteur': id_capteur, 'text': text}
    return self.post_request("/capteurs/" + str(id_capteur) + "/donneesCapteur", donnee)

  def creer_capteur(self, id_erabliere, nom, symbole, afficher_capteur_dashboard):
    capteur = {'nom': nom, 'symbole': symbole, 'afficherCapteurDashboard': afficher_capteur_dashboard, 'idErabliere': id_erabliere }
    return self.post_request(self.path_e + str(id_erabliere) + "/capteurs", capteur)

  def get_request(self, action) -> requests.Response:
    token = self.get_token()
    h = {"Authorization": "Bearer " + str(token)}
    r = requests.get(self.base_url + action, headers = h, timeout = self.timeout, verify = self.verify_ssl)
    return r

  def post_request(self, action, body) -> requests.Response:
    token = self.get_token()
    h = {"Authorization": "Bearer " + str(token), "Content-Type":"Application/json"}
    r = requests.post(self.base_url + action, json = body, headers = h, timeout = self.timeout, verify = self.verify_ssl)
    return r

  def get_headers(self):
    token = self.get_token()
    if (self.auth_provider == None or self.auth_provider == "None"):
      return {}
    elif (self.auth_provider == "ApiKey"):
      return {"X-ErabliereApi-ApiKey": str(token)}
    elif (self.auth_provider == "Identity"):
      return {"Authorization": "Bearer " + str(token)}
    elif (self.auth_provider == "AzureAD"):
      return {"Authorization": "Bearer " + str(token)}
    else:
      raise NameError("The name of the auth_provider is invalid. Must be None, 'ApiKey' or 'Identity' or 'AzureAD'. The value was " + str(self.auth_provider) + ".")
    

  def get_token(self):
    if (self.auth_provider == None or self.auth_provider == "None"):
      return None
    if (self.auth_provider == "ApiKey"):
      self.init_auth_config()
      return self.auth_config["ApiKey"]
    if (self.auth_provider == "Identity"):
      return getAccessTokenIdentity("https://192.168.0.103:5005/connect/token", "raspberrylocal", "secret", verifySsl = False)
    if (self.auth_provider == "AzureAD"):
      if self.auth_config == None:
        self.init_auth_config();
      token = self.aad_token_provider.getAccessToken(self.auth_config)
      return token

    raise NameError("The name of the auth_provider is invalid. Must be None, 'Identity' or 'ApiKey' or 'AzureAD'. The value was " + str(self.auth_provider) + ".")

  def init_auth_config(self):
    auth_path = f"/home/{os.environ['USER']}/.erabliereapi/auth.{self.host}.config"
    if (os.name == "nt"):
      auth_path = f"E:\\config\\python\\aad-client-credentials.{self.host}.json"
    print("Open config from file", auth_path)
    auth_config = open(auth_path,)
    self.auth_config = json.load(auth_config)[0]
    auth_config.close()