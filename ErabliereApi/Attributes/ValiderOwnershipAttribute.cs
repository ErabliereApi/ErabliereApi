﻿using ErabliereApi.Depot.Sql;
using ErabliereApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ErabliereApi.Attributes;

/// <summary>
/// Vérifier si l'utilisateur à les droits d'accès sur la ressource qu'il tente d'accéder ou de modifier
/// en vérifiant le verbe http et les droits dans la table CustomerErablieres
/// </summary>
public class ValiderOwnershipAttribute : ActionFilterAttribute
{
    private readonly string _idParamName;
    private readonly Type? _levelTwoRelationType;

    /// <summary>
    /// Valider les droits d'accès
    /// </summary>
    /// <param name="idParamName">Le nom du paramètre de route pour l'id de l'érablière</param>
    /// <param name="levelTwoRelationType">Type référencé dans l'arboressence des relations</param>
    public ValiderOwnershipAttribute(string idParamName, Type? levelTwoRelationType = null)
    {
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
        var allowAccess = true;

        var dbContext = context.HttpContext.RequestServices.GetRequiredService<ErabliereDbContext>();

        var strId = context.HttpContext.Request.RouteValues[_idParamName]?.ToString();

        if (strId == null)
        {
            throw new InvalidOperationException($"Route value {_idParamName} does not exist");
        }

        var idGuid = Guid.Parse(strId);

        var erabliere = await dbContext.Erabliere.FindAsync(idGuid);

        if (erabliere != null)
        {
            var userService = context.HttpContext.RequestServices.GetRequiredService<IUserService>();

            var customer = await userService.GetCurrentUserWithAccessAsync(erabliere);

            if (customer == null)
            {
                throw new InvalidOperationException("Customer should exist at this point...");
            }

            if (customer.CustomerErablieres == null || customer.CustomerErablieres.Count == 0)
            {
                allowAccess = false;
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
                    var access = customer.CustomerErablieres[i].Access;

                    allowAccess = (access & type) > 0;
                }
            }
        }

        if (allowAccess)
        {
            await base.OnActionExecutionAsync(context, next);
        }
        else
        {
            context.Result = new ForbidResult();
        }
    }
}
