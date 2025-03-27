using Microsoft.AspNetCore.Mvc.Filters;

namespace ErabliereApi.Attributes;

/// <summary>
/// Attribute to log the usage of obsolete actions
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
public class LogObsoleteUsageAttribute : ActionFilterAttribute
{
    /// <inheritdoc />
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        base.OnActionExecuting(context);

        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<LogObsoleteUsageAttribute>>();

        try
        {
            var user = context.HttpContext.User.Identity?.Name ?? "Anonymous";
            var ipAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown IP";
            var userAgent = context.HttpContext.Request.Headers["User-Agent"].ToString();

            logger.LogWarning("Obsolete usage of {DisplayName} by user {User} from IP {IPAddress} with User-Agent {UserAgent}",
                context.ActionDescriptor.DisplayName, user, ipAddress, userAgent);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while logging obsolete usage of {DisplayName}", context.ActionDescriptor.DisplayName);
        }
    }
}
