#!/bin/sh

# exit when any command fails
set -e

# Trust the development root CA when one is mounted. ErabliereAPI serves https
# with a self-signed certificate in docker compose, and this container is one of
# its clients. Nothing is mounted in production, where the API has a real
# certificate, so the block is simply skipped.
if [ -f /https-root/aspnetapp-root-cert.cer ]; then
    echo "trust certificate"
    # The PEM goes to /tmp, not next to the .cer: the container runs unprivileged
    # and the mount point directory belongs to root.
    openssl x509 -inform DER -in /https-root/aspnetapp-root-cert.cer -out /tmp/aspnetapp-root-cert.crt
    cp /tmp/aspnetapp-root-cert.crt /usr/local/share/ca-certificates/
    update-ca-certificates
fi

echo "launching the MCP server over http!"
# exec so the app is PID 1 and receives the docker stop signal
exec dotnet ErabliereApi.Mcp.dll --http
