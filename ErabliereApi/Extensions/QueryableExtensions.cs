using ErabliereApi.Donnees;
using Microsoft.EntityFrameworkCore;

namespace ErabliereApi.Extensions;

/// <summary>
/// Méthodes d'extension sur les IQueryable
/// </summary>
public static class QueryableExtensions
{
    /// <summary>
    /// Take maximal sur le get erablieres
    /// </summary>
    public const int TakeErabliereNbMax = 20;

    /// <summary>
    /// Ajout de l'entête X-ErabliereTotal, ajoute le tri sur IndiceOrdre et le nom puis limite le nombre d'élément retourner si nécessaire
    /// </summary>
    /// <param name="query"></param>
    /// <param name="orderby"></param>
    /// <param name="filter"></param>
    /// <param name="top"></param>
    /// <param name="httpContext"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public static async Task<IQueryable<Erabliere>> AddOrderAndPageInfoAsync(this IQueryable<Erabliere> query, string? orderby, string? filter, int? top, HttpContext httpContext, CancellationToken token)
    {
        httpContext.Response.Headers.Append("X-ErabliereTotal", (await query.CountAsync(token)).ToString());

        if (string.IsNullOrWhiteSpace(orderby))
        {
            query = query.OrderBy(e => e.IndiceOrdre ?? int.MaxValue).ThenBy(e => e.Nom);
        }

        if (string.IsNullOrWhiteSpace(filter))
        {
            if (top.HasValue)
            {
                if (top > TakeErabliereNbMax)
                {
                    query = query.Take(TakeErabliereNbMax);
                }
            }
            else
            {
                query = query.Take(TakeErabliereNbMax);
            }
        }

        return query;
    }
}
