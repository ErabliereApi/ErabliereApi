# ErabliereAPI.Proxy

Client proxy pour l'api ErabliereAPI.

## Generate new version

1. Get nswag studioL ```choco install nswagstudio```
2. Load the nswag file
3. Generate the client
4. Review the errors
   1. Two Guid id for PostNote endpoint
   2. DateTimeOffSet?.Value missing
5. Save the files
6. Update csproj version and release notes
7. Once ready, push the changes to the repository

## How to use

```csharp
var client = new ErabliereAPIProxy("https://localhost:5001", new HttpClient());
var result = await client.GetWeatherAsync();
```