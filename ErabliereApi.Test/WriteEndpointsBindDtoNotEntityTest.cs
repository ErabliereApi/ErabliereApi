using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ErabliereApi.Controllers;
using ErabliereApi.Depot.Sql;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace ErabliereApi.Test;

/// <summary>
/// Test d'architecture (anti over-posting) : aucune action POST/PUT/PATCH d'un contrôleur
/// ne doit lier une entité EF (namespace <c>ErabliereApi.Donnees</c>) comme corps de requête.
/// Il faut utiliser un DTO dédié (<c>ErabliereApi.Donnees.Action.Post</c> / <c>.Put</c>) sans
/// propriété de navigation. Voir la section « No entity binding on write endpoints » du CLAUDE.md.
/// </summary>
public class WriteEndpointsBindDtoNotEntityTest
{
    private readonly ITestOutputHelper _output;

    public WriteEndpointsBindDtoNotEntityTest(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    /// Dette technique connue : contrôleurs qui lient encore une entité EF brute. Ne pas allonger
    /// cette liste — migrer vers un DTO quand on touche à ces contrôleurs.
    /// Format des clés : "NomController.NomMethode(NomType)".
    /// Les entrées restantes sont réservées aux administrateurs (acteurs de confiance, risque
    /// d'over-posting faible) et sont laissées en dette assumée.
    /// </summary>
    private static readonly HashSet<string> ExceptionsConnues = new()
    {
        "ChirpstackController.CreateConfigs(ChirpStackSrvConfig)",
        "ChirpstackController.EditConfigs(ChirpStackSrvConfig)",
        "IpInfoController.ImportIpInfo(IpInfo)",
    };

    [Fact]
    public void AucunEndpointDEcriture_NeLieUneEntite_HorsExceptionsConnues()
    {
        var violations = TrouverViolations().ToList();

        foreach (var v in violations)
        {
            _output.WriteLine(v);
        }

        var nouvellesViolations = violations
            .Where(v => !ExceptionsConnues.Contains(v))
            .ToList();

        Assert.True(
            nouvellesViolations.Count == 0,
            "Ces actions POST/PUT/PATCH lient une entité EF au lieu d'un DTO (risque d'over-posting). " +
            "Créez un DTO Post*/Put* sans propriété de navigation. Voir CLAUDE.md.\n" +
            string.Join("\n", nouvellesViolations));
    }

    /// <summary>
    /// Vérifie que la liste d'exceptions ne contient pas d'entrée périmée (contrôleur déjà
    /// migré vers un DTO). Force le nettoyage de la dette au fur et à mesure.
    /// </summary>
    [Fact]
    public void ExceptionsConnues_NeContiennentPasDEntreePerimee()
    {
        var violations = TrouverViolations().ToHashSet();

        var perimees = ExceptionsConnues.Where(e => !violations.Contains(e)).ToList();

        Assert.True(
            perimees.Count == 0,
            "Ces exceptions ne correspondent plus à une violation réelle (contrôleur migré ?). " +
            "Retirez-les de ExceptionsConnues :\n" + string.Join("\n", perimees));
    }

    private static IEnumerable<string> TrouverViolations()
    {
        var entites = TypesEntites();
        var assembly = typeof(ArbresController).Assembly;

        var controllers = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(ControllerBase).IsAssignableFrom(t));

        foreach (var controller in controllers)
        {
            var methods = controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            foreach (var method in methods)
            {
                if (!EstEndpointDEcriture(method))
                {
                    continue;
                }

                foreach (var parametre in method.GetParameters())
                {
                    var typeLie = TypeModeleLie(parametre.ParameterType);

                    if (typeLie != null && entites.Contains(typeLie))
                    {
                        yield return $"{controller.Name}.{method.Name}({typeLie.Name})";
                    }
                }
            }
        }
    }

    /// <summary>
    /// L'ensemble des types suivis par EF, c.-à-d. exposés comme <c>DbSet&lt;T&gt;</c> sur le
    /// <see cref="ErabliereDbContext" />. Ce sont exactement les types dont le graphe est
    /// traversé par <c>AddAsync</c>/<c>Update</c> — donc à risque d'over-posting.
    /// </summary>
    private static HashSet<Type> TypesEntites()
    {
        return typeof(ErabliereDbContext).GetProperties()
            .Where(p => p.PropertyType.IsGenericType &&
                        p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
            .Select(p => p.PropertyType.GetGenericArguments()[0])
            .ToHashSet();
    }

    private static bool EstEndpointDEcriture(MethodInfo method)
    {
        return method.GetCustomAttributes().Any(a =>
            a is HttpPostAttribute || a is HttpPutAttribute || a is HttpPatchAttribute);
    }

    /// <summary>
    /// Retourne le type de modèle candidat au binding de corps : le type lui-même, ou l'élément
    /// s'il s'agit d'un tableau ou d'une collection générique. Retourne null pour les types simples.
    /// </summary>
    private static Type? TypeModeleLie(Type type)
    {
        if (type.IsArray)
        {
            return TypeModeleLie(type.GetElementType()!);
        }

        if (type.IsGenericType && typeof(IEnumerable).IsAssignableFrom(type))
        {
            var arg = type.GetGenericArguments().FirstOrDefault();
            return arg != null ? TypeModeleLie(arg) : null;
        }

        if (type.IsClass && type != typeof(string))
        {
            return type;
        }

        return null;
    }
}
