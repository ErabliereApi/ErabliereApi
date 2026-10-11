namespace ErabliereApi.Middlewares;

/// <summary>
/// Middleware qui ajoute les entêtes de sécurité HTTP à toutes les réponses : fichiers statiques,
/// contrôleurs, erreurs et repli vers l'application Angular.
/// </summary>
/// <remarks>
/// Il doit être enregistré tôt dans le pipeline. Placé après <c>UseEndpoints</c>, il ne s'exécute
/// jamais pour une requête qui correspond à un contrôleur, et les réponses de l'API sortent sans entêtes.
/// </remarks>
public class SecurityHeadersMiddleware
{
    private static readonly string[] cspPolicy =
    [
        "default-src 'self'",
        "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://api.mapbox.com",
        "style-src 'self' 'unsafe-inline' https://api.mapbox.com",
        "img-src 'self' data: blob:",
        "font-src 'self'",
        "connect-src 'self' https://api.mapbox.com https://events.mapbox.com https://login.microsoftonline.com",
        "frame-src 'none' https://login.microsoftonline.com",
        "object-src 'none'",
        "base-uri 'self'",
        "form-action 'self' https://login.microsoftonline.com",
        "frame-ancestors 'self' https://login.microsoftonline.com",
        "worker-src 'self' blob:"
    ];

    private static readonly string cspHeaderValue = string.Join("; ", cspPolicy);

    private readonly RequestDelegate _next;
    private readonly string _xFrameOptions;
    private readonly string _crossOriginOpenerPolicy;

    /// <summary>
    /// Constructeur par défaut
    /// </summary>
    /// <param name="next"></param>
    /// <param name="config"></param>
    public SecurityHeadersMiddleware(RequestDelegate next, IConfiguration config)
    {
        _next = next;

        // SAMEORIGIN par défaut : le renouvellement silencieux de MSAL charge l'application
        // dans un iframe caché d'elle-même, ce que DENY empêcherait.
        _xFrameOptions = string.Equals(config["USE_X_FRAME_OPTIONS_DENY"]?.Trim(), bool.TrueString, StringComparison.InvariantCultureIgnoreCase)
            ? "DENY"
            : "SAMEORIGIN";

        var coop = config["Cross-Origin-Opener-Policy"];

        _crossOriginOpenerPolicy = string.IsNullOrWhiteSpace(coop) ? "same-origin-allow-popups" : coop.Trim();
    }

    /// <summary>
    /// Enregistre l'ajout des entêtes au démarrage de la réponse
    /// </summary>
    public Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(state =>
        {
            var headers = ((HttpContext)state).Response.Headers;

            headers["Content-Security-Policy"] = cspHeaderValue;
            headers["X-Frame-Options"] = _xFrameOptions;
            headers["X-Content-Type-Options"] = "nosniff";
            // 0 désactive l'ancien filtre XSS des navigateurs, qui introduisait ses propres failles. La CSP le remplace.
            headers["X-XSS-Protection"] = "0";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            // geolocation=(self) : la carte de tubulure localise l'utilisateur sur le terrain.
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(self), payment=(), usb=(), bluetooth=()";
            headers["Cross-Origin-Opener-Policy"] = _crossOriginOpenerPolicy;
            headers["Cross-Origin-Resource-Policy"] = "cross-origin";

            return Task.CompletedTask;
        }, context);

        return _next(context);
    }
}
