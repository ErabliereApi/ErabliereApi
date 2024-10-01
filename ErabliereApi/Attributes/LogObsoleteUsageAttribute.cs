using Microsoft.AspNetCore.Mvc.Filters;

namespace ErabliereApi.Attributes;

/// <summary>
/// Attribute to log the usage of obsolete actions
/// </summary>
public class LogObsoleteUsageAttribute : ActionFilterAttribute
{
    /// <inheritdoc />
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        base.OnActionExecuting(context);

        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<LogObsoleteUsageAttribute>>();

        logger.LogWarning("Obsolete usage of {DisplayName}", context.ActionDescriptor.DisplayName);
    }
}
