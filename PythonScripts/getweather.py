from proxy.erabliere_api_proxy import ErabliereApiProxy
import requests
import os
import sys

apiKeyFilePath = sys.argv[1]
locationKey = sys.argv[2]

# Validate against path traversal attack

# If the file apiKey exist, read it
if os.path.isfile(apiKeyFilePath):
    with open(apiKeyFilePath, 'r') as f:
        apiKey = f.read()

url = "https://dataservice.accuweather.com/forecasts/v1/daily/5day/" + locationKey + "?language=fr"

print("Requesting " + url)
# Send the request to AccuWeather
authorizeHeader = "Bearer " + apiKey
headers = {
    "Authorization": authorizeHeader
}
response = requests.get(url, headers=headers)
print(response.status_code)

# Parse the json
data = response.json()

# Send the temperature to ErabliereAPI using ErabliereApiProxy
url = sys.argv[3]
idCapteur = sys.argv[4]
proxy = ErabliereApiProxy(url, "AzureAD")
print("Sending temperature to ErabliereAPI")
response = proxy.envoyer_donnee_capteur_v2(idCapteur, int(data[0]["Temperature"]["Metric"]["Value"]), data[0]["WeatherText"])

# Print the response
print(response.status_code)

# If the program failed, return an error code
if response.status_code != 200:
    sys.exit(1)

