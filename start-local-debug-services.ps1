git status
git pull

Start-Process stripe -ArgumentList "listen", "--forward-to", "localhost:5000/Checkout/Webhook"

Start-Sleep 5

# check if stripe is running
$stripe = Get-Process | Where-Object { $_.Name -eq "stripe" }
if ($null -eq $stripe) {
    stripe login

    Start-Process stripe -ArgumentList "listen", "--forward-to", "localhost:5000/Checkout/Webhook"
}

Set-Location .\ErabliereApi\

Start-Process dotnet -ArgumentList "watch", "run", "$PWD\ErabliereApi.csproj", "ASPNETCORE_ENVIRONMENT=Development", "ASPNETCORE_HTTP_PORTS=5000", "ASPNETCORE_HTTPS_PORTS=5001", " --no-hot-reload"

Set-Location ..
Set-Location ErabliereIU

Start-Process npm.cmd -ArgumentList "start"

Set-Location ..

code .

# if the parent folder contains a folder name EmailImagesObserver, then start the EmailImagesObserver server in a new process, also in watch mode

$emailImagesObserver = Get-ChildItem -Path ..\ -Directory -Filter "EmailImagesObserver" | Select-Object -expand FullName
if ($null -ne $emailImagesObserver) {
    Set-Location "$emailImagesObserver\BlazorApp"
    Start-Process dotnet -ArgumentList "watch", "run", "$PWD\BlazorApp.csproj", " --no-hot-reload"

    Set-Location ..\..\ErabliereApi
}

# if the parent folder contains a folder name ErabliereWS, then start the dotnet app in a new process, also in watch mode

$erabliereWS = Get-ChildItem -Path ..\ -Directory -Filter "ErabliereWS" | Select-Object -expand FullName
if ($null -ne $erabliereWS) {
    Set-Location $erabliereWS\ErabliereWS
    Start-Process dotnet -ArgumentList "watch", "run", "$PWD\ErabliereWS\ErabliereWS.csproj", " --no-hot-reload"

    Set-Location ..\..\ErabliereApi
}

# if the parent folder contains a folder name JeuxDonneesErabliereAPI, then start the dotnet app in a new process, also in watch mode

$jeuxDonneesErabliereAPI = Get-ChildItem -Path ..\ -Directory -Filter "JeuxDonneesErabliereAPI" | Select-Object -expand FullName
if ($null -ne $jeuxDonneesErabliereAPI) {
    Set-Location $jeuxDonneesErabliereAPI\JeuxDonneesErabliereAPI
    Start-Process dotnet -ArgumentList "watch", "run", "$PWD\JeuxDonneesErabliereAPI.csproj", " --no-hot-reload"

    Set-Location ..\..\ErabliereApi
}