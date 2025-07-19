# GenerateProxy.ps1

# Check if NSwag is installed
if (-not (Get-Command nswag -ErrorAction SilentlyContinue)) {
    Write-Error "NSwag is not installed or not in PATH. Install it with 'dotnet tool install --global NSwag.ConsoleCore'."
    exit 1
}

# Run NSwag to generate the proxy
# nswag run

# if ($LASTEXITCODE -eq 0) {
#     Write-Host "Proxy generation completed successfully."
# } else {
#     Write-Error "Proxy generation failed."
#     exit $LASTEXITCODE
# }

# In the ErabliereAPIClient.cs file, replace all x_ddr.ToString("s") with x_ddr.Value.ToString("s")
$filePath = "ErabliereAPIClient.cs"
if (Test-Path $filePath) {
    (Get-Content $filePath) -replace 'x_ddr\.ToString\("s"\)', 'x_ddr.Value.ToString("s")' | Set-Content $filePath
    Write-Host "Replaced x_ddr.ToString('s') with x_ddr.Value.ToString('s') in $filePath."
} else {
    Write-Error "File $filePath does not exist."
}

# When two lines are starting with "<param name="id">L'id de la note si le client désire l'initialiser</param>" replace the second line with "<param name="idNote">L'id de la note si le client désire l'initialiser</param>"
$filePath = "ErabliereAPIClient.cs"
if (Test-Path $filePath) {
    $content = Get-Content $filePath
    $newContent = $content -replace '<param name="id">L''id de la note si le client désire l''initialiser</param>', '<param name="idNote">L''id de la note si le client désire l''initialiser</param>'
    Set-Content $filePath $newContent
    Write-Host "Replaced parameter 'id' with 'idNote' in $filePath."
} else {
    Write-Error "File $filePath does not exist."
}

# Replace "id, id, " with "id, idNote, "
$filePath = "ErabliereAPIClient.cs"
if (Test-Path $filePath) {
    $updatedContent = (Get-Content $filePath).Replace("System.Guid id, System.Guid? id, ", "System.Guid id, System.Guid? idNote, ")
    Set-Content $filePath $updatedContent
    Write-Host "Replaced 'id, id, ' with 'id, idNote, ' in $filePath."
} else {
    Write-Error "File $filePath does not exist."
}