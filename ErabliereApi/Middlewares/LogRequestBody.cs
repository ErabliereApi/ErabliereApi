using Microsoft.AspNetCore.Http;
using System.Text;

namespace ErabliereApi.Middlewares;

public class LogRequestBodyMiddleware
{
    private readonly RequestDelegate _next;

    public LogRequestBodyMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 1. Activer le buffering pour permettre de relire le flux
        context.Request.EnableBuffering();

        // 2. Lire le corps de la requête
        // On utilise leaveOpen: true pour ne pas fermer le flux de la requête prématurément
        using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true))
        {
            var body = await reader.ReadToEndAsync();

            if (!string.IsNullOrWhiteSpace(body))
            {
                Console.WriteLine("\n--- Middleware: Request Body Begin ---");
                Console.WriteLine(body);
                Console.WriteLine("--- Middleware: Request Body End ---\n");
            }

            // 3. IMPORTANT : Rembobiner le flux pour les middlewares suivants (et le Model Binder)
            context.Request.Body.Position = 0;
        }

        // Passer au middleware suivant dans le pipeline
        await _next(context);
    }
}