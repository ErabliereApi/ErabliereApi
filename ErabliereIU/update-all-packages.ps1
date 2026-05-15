param(
    $coolDown = 14
)

Write-Host "Updating angular"
. .\update-angular-version.ps1

Write-Host "Previewing updates..."
npm i -g npm-check-updates
ncu --cooldown $coolDown

Write-Host "Do you want to proceed to the update? (y/n) "
$yn = Read-Host
if ($yn -eq 'y') {
  ncu -u --cooldown $coolDown
}

npm install

Write-Host "Watchout when passing from babel-loader ^9.2.1  →  ^10.0.0. It may not compile."