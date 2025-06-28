# sudo apt install libraspberrypi-bin

# available command
#vcgencmd measure_temp
#vcgencmd get_throttled
#vcgencmd measure_volts
#vcgencmd get_mem arm
#vcgencmd get_mem gpu

# free -mh

# inspiration
# https://www.cloudsavvyit.com/9996/monitoring-temperature-on-the-raspberry-pi/
# https://medium.com/@kevalpatel2106/monitor-the-core-temperature-of-your-raspberry-pi-3ddfdf82989f

# To make the script work you may need to do
# sudo usermod -aG video <username> (source: https://chewett.co.uk/blog/258/vchi-initialization-failed-raspberry-pi-fixed/)
# and reboot the PI

import os
import time
import sys

def measure_temp():
  temp = os.popen("vcgencmd measure_temp").readline()
  temp = (temp.replace("temp=",""))
  return float(temp.split("'")[0])

t = measure_temp()

print(t)

if len(sys.argv) == 1:
  exit()

from proxy.erabliere_api_proxy import ErabliereApiProxy

authType = None

if len(sys.argv) > 2:
  authType = sys.argv[2]

proxy = ErabliereApiProxy(sys.argv[1], authType)

r = proxy.envoyer_donnee_capteur_v2(sys.argv[len(sys.argv)-1], t)

print(r)
