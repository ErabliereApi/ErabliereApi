param(
    [string]$storybookVersion = "latest"
)

Write-Host "Running npx storybook@$storybookVersion upgrade --yes"
"\n" | npx storybook@$storybookVersion upgrade

Write-Host "Running npm dedupe"
npm dedupe

Write-Host "Update chromatic"
npm install chromatic@latest