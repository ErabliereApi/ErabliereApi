﻿using Microsoft.AspNetCore.Mvc.Filters;
using StackExchange.Profiling;

namespace ErabliereApi;

/// <summary>
/// IAsyncActionFilter pour MiniProfiler
/// </summary>
public class MiniProfilerAsyncLogger : IAsyncActionFilter
{
    /// <inheritdoc />
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<MiniProfilerAsyncLogger>>();

        using (MiniProfiler.Current.Step(context.ActionDescriptor.DisplayName))
        {
            await next();
        }

        logger.LogInformation(MiniProfiler.Current.RenderPlainText());
    }
}
