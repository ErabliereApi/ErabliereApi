New-Item -ItemType Directory -Path ./TestResults -ErrorAction SilentlyContinue
Remove-Item -Path ./TestResults/* -Recurse -Force -ErrorAction SilentlyContinue

dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults

$coverageFiles = Get-ChildItem -Path ./TestResults -Recurse -Filter "coverage.cobertura.xml"

foreach ($file in $coverageFiles) {
    Write-Host "Code coverage report generated at: $($file.FullName)"
}

reportgenerator `
    -reports:"TestResults\**\coverage.cobertura.xml" `
    -targetdir:"coveragereport" `
    -reporttypes:Html

Write-Host "HTML code coverage report generated at: ./coveragereport/index.html"