namespace ErabliereApi.Extensions;

/// <summary>
/// Extension methods for file handling.
/// </summary>
public static class FileExtension
{
    /// <summary>
    /// Converts an IFormFile to a byte array asynchronously.
    /// </summary>
    /// <param name="file">The file to convert.</param>
    /// <param name="token">The cancellation token to observe while waiting for the task to complete.</param>
    public async static Task<byte[]> ToByteArray(this IFormFile file, CancellationToken token)
    {
        using var ms = new MemoryStream();

        await file.CopyToAsync(ms, token);

        return ms.ToArray();
    }
}