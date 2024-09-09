using ErabliereApi.Depot.Sql;
using ErabliereApi.Donnees.Action.Post;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using ErabliereApi.Controllers;
using Microsoft.Extensions.Localization;

namespace ErabliereApi.Extensions;

/// <summary>
/// Extension pour la classe PostErabliere
/// </summary>
public static class PostErabliereExtension 
{
    /// <summary>
    /// Valide les données de la requête POST
    /// </summary>
    /// <param name="postErabliere"></param>
    /// <param name="ModelState"></param>
    /// <param name="_context"></param>
    /// <param name="_localizer"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public static async Task<(bool, IActionResult?)> ValidateAsync(
        this PostErabliere postErabliere, 
        ModelStateDictionary ModelState, 
        ErabliereDbContext _context,
        IStringLocalizer<ErablieresController> _localizer,
        CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(postErabliere.Nom))
        {
            ModelState.AddModelError(nameof(postErabliere.Nom), _localizer["NomVide"]);

            return (false, new BadRequestObjectResult(new ValidationProblemDetails(ModelState)));
        }
        if (await _context.Erabliere.AnyAsync(e => e.Nom == postErabliere.Nom, token))
        {
            ModelState.AddModelError(nameof(postErabliere.Nom), string.Format(_localizer["NomExiste"], postErabliere.Nom));

            return (false, new BadRequestObjectResult(new ValidationProblemDetails(ModelState)));
        }
        if (postErabliere.Id != null) 
        {
            var e = await _context.Erabliere.FindAsync([postErabliere.Id], token);

            if (e != null) 
            {
                ModelState.AddModelError(nameof(postErabliere.Id), string.Format(_localizer["IdExiste"], postErabliere.Id));
        
                return (false, new ConflictObjectResult(new ValidationProblemDetails(ModelState)));
            }
        }

        return (true, null);
    }
}