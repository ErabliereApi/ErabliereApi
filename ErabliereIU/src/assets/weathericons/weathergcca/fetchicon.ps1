$baseUrl = "https://weather.gc.ca/weathericons/small"
$index = 0

while ($true) {
    # Format du numéro : 01, 02, 03, etc.
    $fileName = "{0:D2}.png" -f $index
    $outFileName = "{0:D2}-s.png" -f $index
    $url = "$baseUrl/$fileName"
    $output = Join-Path -Path (Get-Location) -ChildPath $outFileName

    Write-Host "Téléchargement de $url ..."

    try {
        Invoke-WebRequest -Uri $url -OutFile $output -ErrorAction Stop
        Write-Host " -> OK : $fileName sauvegardé"
    }
    catch {
        Write-Host " -> Fin : $fileName n'existe pas. Arrêt de la boucle."
        break
    }

    $index++
}
