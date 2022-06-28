﻿using ErabliereApi.Depot.Sql;
using ErabliereApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ErabliereApi.Attributes;

/// <summary>
/// Vérifier si l'utilisateur à les droits d'accès sur la ressource qu'il tente d'accéder ou de modifier
/// en vérifiant le verbe http et les droits dans la table CustomerErablieres
/// </summary>
public class ValiderOwnershipAtributes : ActionFilterAttribute
{
    private readonly string _idErabliereParamName;

    /// <summary>
    /// Valider les droits d'accès
    /// </summary>
    /// <param name="idErabliereParamName">Le nom du paramètre de route pour l'id de l'érablière</param>
    public ValiderOwnershipAtributes(string idErabliereParamName)
    {
        _idErabliereParamName = idErabliereParamName;
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

        var erabliere = await dbContext.
            Erabliere.FindAsync(context.HttpContext.Request.RouteValues[_idErabliereParamName]);

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

                    allowAccess = !((access & type) > 0);
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
