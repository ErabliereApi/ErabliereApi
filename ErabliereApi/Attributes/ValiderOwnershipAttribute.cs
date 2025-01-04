using ErabliereApi.Depot.Sql;
using ErabliereApi.Donnees;
using ErabliereApi.Donnees.Ownable;
using ErabliereApi.Extensions;
using ErabliereApi.Services.Users;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Distributed;

namespace ErabliereApi.Attributes;

/// <summary>
/// Vérifier si l'utilisateur à les droits d'accès sur la ressource qu'il tente d'accéder ou de modifier
/// en vérifiant le verbe http et les droits dans la table CustomerErablieres
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class ValiderOwnershipAttribute : ActionFilterAttribute
{
    private readonly string _idParamName;
    private readonly Type? _levelTwoRelationType;
    private ILogger<ValiderOwnershipAttribute>? _logger;

    /// <summary>
    /// Valider les droits d'accès
    /// </summary>
    /// <param name="idParamName">Le nom du paramètre de route pour l'id de l'érablière</param>
    /// <param name="levelTwoRelationType">Type référencé dans l'arboressence des relations</param>
    public ValiderOwnershipAttribute(string idParamName, Type? levelTwoRelationType = null)
    {
        if (levelTwoRelationType != null &&
            !Array.Exists(levelTwoRelationType.GetInterfaces(), i => i == typeof(IErabliereOwnable)))
        {
            throw new ArgumentException($"The type of arg {nameof(levelTwoRelationType)} must implement {nameof(ILevelTwoOwnable<IErabliereOwnable>)}");
        }

        _idParamName = idParamName;
        _levelTwoRelationType = levelTwoRelationType;
    }

    /// <summary>
    /// Verif if the user has the necessary access right
    /// </summary>
    /// <param name="context"></param>
    /// <param name="next"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var strId = (context.HttpContext.Request.RouteValues[_idParamName]?.ToString()) ?? 
            throw new InvalidOperationException($"Route value {_idParamName} does not exist");
        
        var allowAccess = true;
        var config = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();

        if (config.IsAuthEnabled())
        {
            allowAccess = await CheckAllowAccess(context, allowAccess, strId);
        }

        if (allowAccess)
        {
            await base.OnActionExecutionAsync(context, next);
        }
        else
        {
            var forbidenReasonMessage = $"Access Denied for {context.HttpContext.Request.Method} {context.HttpContext.Request.Path}";
            context.HttpContext.Response.Headers["X-ErabliereApi-ForbidenReason"] = forbidenReasonMessage;
            context.Result = new ForbidResult();
            if (_logger == null)
            {
                _logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<ValiderOwnershipAttribute>>();
            }
            using var scope = context.HttpContext.RequestServices.CreateScope();
            _logger.LogWarning("Access Denied for {Method} {Path} for user {User}", 
                context.HttpContext.Request.Method.Sanatize(), 
                context.HttpContext.Request.Path.Value.Sanatize(),
                UsersUtils.GetUniqueName(scope, context.HttpContext.User));
        }
    }

    private async Task<bool> CheckAllowAccess(ActionExecutingContext context, bool allowAccess, string strId)
    {
        var dbContext = context.HttpContext.RequestServices.GetRequiredService<ErabliereDbContext>();
        var cache = context.HttpContext.RequestServices.GetRequiredService<IDistributedCache>();

        Erabliere? erabliere = await GetErabliere(dbContext, cache, strId, context.HttpContext.RequestAborted);

        // Valider les droits d'accès sur l'érablière
        // Si l'érablière a été trouvé
        // Si l'érablière est publique et que l'accès est en lecture, l'accès est autorisé
        if (erabliere == null)
        {
            context.Result = new NotFoundObjectResult(new { Message = $"{_levelTwoRelationType?.Name} {strId} n'existe pas." });
        }
        else if (!(erabliere.IsPublic && context.HttpContext.Request.Method == "GET"))
        {
            allowAccess = await InnerCheck(context, allowAccess, erabliere);
        }

        return allowAccess;
    }

    private async Task<bool> InnerCheck(ActionExecutingContext context, bool allowAccess, Erabliere erabliere)
    {
        var userService = context.HttpContext.RequestServices.GetRequiredService<IUserService>();

        var customer = await userService.GetCurrentUserWithAccessAsync(erabliere, context.HttpContext.RequestAborted);

        if (customer == null)
        {
            throw new InvalidOperationException("Customer should exist at this point...");
        }

        if (customer.CustomerErablieres == null || customer.CustomerErablieres.Count == 0)
        {
            allowAccess = false;
            if (_logger == null)
            {
                _logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<ValiderOwnershipAttribute>>();
            }
            if (customer.CustomerErablieres == null)
            {
                _logger.LogWarning("Customer {CustomerId} has no access to any erabliere as customer.CustomerErablieres == null", customer.Id);
            }
            else
            {
                _logger.LogWarning("Customer {CustomerId} has no access to any erabliere as customer.CustomerErablieres.Count == 0", customer.Id);
            }
        }
        else
        {
            var type = context.HttpContext.Request.Method switch
            {
                "GET" => 1,
                "POST" => 2,
                "PUT" => 4,
                "DELETE" => 8,
                _ => throw new InvalidOperationException($"Ownership not implement for HTTP Verb {context.HttpContext.Request.Method}"),
            };

            for (int i = 0; i < customer.CustomerErablieres.Count && allowAccess; i++)
            {
                if (customer.CustomerErablieres[i].IdErabliere != erabliere.Id)
                {
                    continue;
                }

                var access = customer.CustomerErablieres[i].Access;

                allowAccess = (access & type) > 0;
            }

            if (!allowAccess)
            {
                if (_logger == null)
                {
                    _logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<ValiderOwnershipAttribute>>();
                }
                _logger.LogWarning("Customer {CustomerId} has no access to erabliere {ErabliereId} for {Method}", customer.Id, erabliere.Id, context.HttpContext.Request.Method.Sanatize());
            }
        }

        return allowAccess;
    }

    private async Task<Erabliere?> GetErabliere(ErabliereDbContext context, IDistributedCache cache, string strId, CancellationToken token)
    {
        var idGuid = Guid.Parse(strId);

        if (_levelTwoRelationType != null)
        {
            var entity = await context.FindAsync(_levelTwoRelationType, [idGuid], token);

            if (entity == null)
            {
                return null;
            }

            var instance = entity as IErabliereOwnable;

            if (instance == null)
            {
                throw new InvalidOperationException($"type {entity.GetType().Name} cannot be convert into {nameof(IErabliereOwnable)}");
            }

            if (!instance.IdErabliere.HasValue)
            {
                return null;
            }

            idGuid = instance.IdErabliere.Value;
        }

        var erabliere = await cache.GetAsync<Erabliere>($"Erabliere_{idGuid}", token);

        if (erabliere == null) 
        {
            erabliere = await context.Erabliere.FindAsync([idGuid], token);

            if (erabliere != null) 
            {
                await cache.SetAsync($"Erabliere_{idGuid}", erabliere, token);
            }
        }

        return erabliere;
    }
}
