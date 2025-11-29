using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ErabliereApi.OperationFilter;

/// <summary>
/// Filter to remove most of the content media type
/// </summary>
public class MediaTypeOperationFilter : IOperationFilter
{
    /// <inheritdoc />
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Remove most of the content media type in request
        CleanupRequestContentTypes(operation);

        // Remove most of the content media type in response
        CleanupResponseContentType(operation);
    }

    private static void CleanupRequestContentTypes(OpenApiOperation operation)
    {
        if (operation.RequestBody?.Content != null)
        {
            var keys = operation.RequestBody.Content.Keys.ToArray();

            operation.RequestBody.Content.Remove("application/xml");
            operation.RequestBody.Content.Remove("text/json");
            operation.RequestBody.Content.Remove("text/plain");
            operation.RequestBody.Content.Remove("application/*+json");

            foreach (var key in keys)
            {
                if (key.Contains("odata") || key.Contains("IEEE"))
                {
                    operation.RequestBody.Content.Remove(key);
                }
            }
        }
    }

    private static void CleanupResponseContentType(OpenApiOperation operation)
    {
        if (operation.Responses == null)
        {
            return;
        }

        foreach (var r in operation.Responses.Select(r => r.Value))
        {
            if (r?.Content != null)
            {
                var keys = r.Content.Keys.ToArray();

                r.Content.Remove("application/xml");
                r.Content.Remove("text/json");
                r.Content.Remove("text/plain");
                r.Content.Remove("application/octet-stream");

                foreach (var key in keys)
                {
                    if (key.Contains("odata") || key.Contains("IEEE"))
                    {
                        r.Content.Remove(key);
                    }
                }
            }
        }
    }
}
