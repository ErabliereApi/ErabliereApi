# Source : https://opensource.com/article/20/11/motion-detection-raspberry-pi

from gpiozero import MotionSensor
import json
import requests
import pytz
import threading
import sys
from datetime import datetime as dt
from datetime import timedelta as td
from datetime import timezone
from time import sleep, time
from apscheduler.schedulers.background import BackgroundScheduler
from auth.getAccessTokenAAD import getAccessToken

# Raspberry Pi GPIO pin config
sensor = MotionSensor(14)

# Detection dompeux settings
threshold_seconds = 10
min_element = 4
collect = []
scheduler = BackgroundScheduler()

# Api url
urlBase = "https://erabliereapi.freddycoder.com"
if len(sys.argv) > 1:
    urlBase = sys.argv[1]

# Erabliere id
idErabliere = "1"
if len(sys.argv) > 2:
    idErabliere = sys.argv[2]

def send_data():
    print('dompeux is over, sending data...')
    proxy = ErabliereApiProxy(urlBase, "AzureAD")
    action = "/erablieres/" + idErabliere + "/dompeux"
    donnees = {'idErabliere': int(idErabliere),
               'dd': collect[0].isoformat(),
               'df': collect[len(collect)-1].isoformat()}

    if donnees['dd'] == donnees['df']:
        print("Not a real donnees. Edge case of the script and the sensor")
    else:
        r = proxy.envoyer_dompeux(action, donnees['dd'], donnees['df'])
        print(r.status_code)
        print(r.text)
        print('done.')

def on_motion():
    de = dt.now(timezone.utc).astimezone()
    print((de - td(hours=5)).strftime("%Y-%m-%d %H:%M:%S"), 'Motion detected!')
    if len(collect) > 0 and de - collect[len(collect)-1] > td(seconds=threshold_seconds):
        print("clear", len(collect), "data.")
        collect.clear()
    collect.append(de)

def no_motion():
    print((dt.utcnow() - td(hours=5)).strftime("%Y-%m-%d %H:%M:%S"), 'nm')
    if len(collect) >= min_element:
        print("sending data in", threshold_seconds, "if no more movement")
        if len(collect) > min_element:
            try:
                scheduler.remove_job('send_dompeux')
            except:
                print("Something went wrong when removing job 'send_dompeux'")
        scheduler.add_job(send_data,
                          'date',
                          id='send_dompeux',
                          next_run_time=dt.utcnow() + td(seconds=threshold_seconds))
    else:
        print("need", min_element - len(collect), "more movement to interprete this movement as dompeux")

print('* Setting up...')

print('* Do not move, setting up the PIR sensor...')
sensor.wait_for_no_motion()

print('* Device ready! ', end='', flush=True)

sensor.when_motion = on_motion
sensor.when_no_motion = no_motion
scheduler.start()
print('Press Ctrl+C to exit\n\n')
sleep(60*60*24*200)
