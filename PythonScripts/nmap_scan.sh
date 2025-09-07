#!/bin/bash

# Run nmap scan and output to result.xml
nmap -sV 192.168.50.0/24 -oX result.xml

# Read the result as text
RESULT=$(cat result.xml)

# Send the result via HTTP PUT to erabliereapi
curl -X PUT \
    -H "Content-Type: text/plain" \
    --data-binary "$RESULT" \
    https://erabliereapi.freddycoder.com/erablieres/$idErabliere/appareil/nmapscan