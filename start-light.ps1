[CmdletBinding()]
param (
    [Parameter()]
    [boolean]$startStripe = $false
)

if ($startStripe) {
    Start-Process stripe -ArgumentList "listen", "--forward-to", "localhost:5000/Checkout/Webhook"
}

Set-Location .\ErabliereApi\

Start-Process dotnet -ArgumentList "watch", "run", "$PWD\ErabliereApi.csproj", "ASPNETCORE_ENVIRONMENT=Development", "ASPNETCORE_HTTP_PORTS=5000", "ASPNETCORE_HTTPS_PORTS=5001", "EmailImageObserverUrl= ", " --no-hot-reload"

Set-Location ..
Set-Location ErabliereIU

Start-Process npm.cmd -ArgumentList "start"

Set-Location ..