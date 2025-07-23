Write-Host "Previewing updates..."
npm i -g npm-check-updates
ncu

Write-Host "Updating angular"
. .\update-angular-version.ps1

ncu -u

npm install

Write-Host "Watchout when passing from babel-loader ^9.2.1  →  ^10.0.0. It may not compile."