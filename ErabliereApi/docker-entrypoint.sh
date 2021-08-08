#!/bin/sh

# exit when any command fails
set -e

echo "trust certificate"
# trust dev root CA
openssl x509 -inform DER -in /https-root/aspnetapp-root-cert.cer -out /https-root/aspnetapp-root-cert.crt
cp /https-root/aspnetapp-root-cert.crt /usr/local/share/ca-certificates/
update-ca-certificates

echo "waiting 30 second to let the database start"
sleep 30

echo "launching the app!"
# start the app
dotnet ErabliereApi.dll
