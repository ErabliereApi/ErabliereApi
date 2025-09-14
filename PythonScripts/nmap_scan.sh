#!/bin/bash

set -euo pipefail

print_usage() {
  echo "Usage: $0 -i <erabliere_id> -n 192.168.1.0/24"
  exit 2
}

echo "Starting nmap scan script..."

ID_ERABLIERE=""
NETWORK_TO_SCAN=""
while getopts ":i:n:h-:" opt; do
  echo "Processing option: $opt"
  case "$opt" in
    i) ID_ERABLIERE="$OPTARG" ;;
    n) NETWORK_TO_SCAN="$OPTARG" ;;
    h) print_usage ;;
    -)
      case "$OPTARG" in
        id) ID_ERABLIERE="${!OPTIND}"; OPTIND=$((OPTIND + 1)) ;;
        network) NETWORK_TO_SCAN="${!OPTIND}"; OPTIND=$((OPTIND + 1)) ;;
        help) print_usage ;;
        *) echo "Unknown option --$OPTARG"; print_usage ;;
      esac
      ;;
    :) echo "Option -$OPTARG requires an argument."; print_usage ;;
    ?) echo "Invalid option: -$OPTARG"; print_usage ;;
  esac
done

echo "Erabliere ID: $ID_ERABLIERE"
echo "Network to scan: $NETWORK_TO_SCAN"

[ -n "$ID_ERABLIERE" ] || print_usage
[ -n "$NETWORK_TO_SCAN" ] || print_usage

EXECUTION_DATE=$(date +%Y%m%d-%H%M%S)

mkdir -p ~/nmapscan

# Run nmap scan and output to result.xml
nmap -sV $NETWORK_TO_SCAN -oX ~/nmapscan/nmap-scan-result-$EXECUTION_DATE.xml

# Read the result as text
RESULT=$(cat ~/nmapscan/nmap-scan-result-$EXECUTION_DATE.xml)

ERABLIEREAPI_KEY=$(awk -F'"' '/"ApiKey"/{print $4}' ~/.erabliereapi/auth.erabliereapi.freddycoder.com.config)

# Send the result via HTTP PUT to erabliereapi
curl -X PUT \
    -H "Content-Type: text/xml" \
    -H "X-ErabliereApi-ApiKey: $ERABLIEREAPI_KEY" \
    -d $RESULT \
    https://erabliereapi.freddycoder.com/erablieres/$ID_ERABLIERE/appareil/nmapscan

echo "Nmap scan result sent to ErabliereApi for erabliere ID $ID_ERABLIERE"
echo "Result saved to ~/nmapscan/nmap-scan-result-$EXECUTION_DATE.xml"
echo "Execution date: $EXECUTION_DATE"
echo "Now you can delete old scans in ~/nmapscan if needed."
echo "Done."