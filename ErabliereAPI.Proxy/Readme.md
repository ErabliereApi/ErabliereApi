# ErabliereAPI.Proxy

Client proxy pour l'api ErabliereAPI.

## Generate new version

1. Get nswag studioL ```choco install nswagstudio```
2. Open the nswag file
   1. Clic on create local copy to refresh the openapi spec
   2. Clic on Generate Outputs
   3. Clic on Generate Files
   4. Close nswag studio and accept to save the nswag file
3. Run the GenerateProxy.ps1 script to edit files and code that require changes
4. Save the files nswag file
5. Update csproj version and release notes
6. Once ready, add, commit and push the changes to the repository

## How to use

```csharp
var client = new ErabliereAPIProxy("https://localhost:5001", new HttpClient());
var result = await client.ErablieresAllAsync(null, null, null, null, null, null);
```