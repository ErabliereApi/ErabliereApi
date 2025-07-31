# ErabliereAPI.Proxy

Client proxy pour l'api ErabliereAPI.

## Generate new version

1. Get nswag studioL ```choco install nswagstudio```
2. Load the nswag file
3. Generate the client
4. Run the GenerateProxy.ps1 script to edit files and code that require changes
5. Save the files nswag file
6. Update csproj version and release notes
7. Once ready, add, commit and push the changes to the repository

## How to use

```csharp
var client = new ErabliereAPIProxy("https://localhost:5001", new HttpClient());
var result = await client.ErablieresAllAsync(null, null, null, null, null, null);
```