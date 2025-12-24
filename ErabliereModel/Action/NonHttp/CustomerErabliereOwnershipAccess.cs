using System;

namespace ErabliereApi.Donnees.Action.NonHttp;

/// <summary>
/// Classe utilisé pour la projection afin d'obtenir l'information sur les accès
/// </summary>
public class CustomerErabliereOwnershipAccess
{
    /// <summary>
    /// Clé étrangère de l'utilisateur
    /// </summary>
    public Guid? IdCustomer { get; set; }

    /// <summary>
    /// Clé étrangère de l'érablière
    /// </summary>
    public Guid? IdErabliere { get; set; }

    /// <inheritdoc cref="CustomerErabliere.Access" />
    public byte Access { get; set; }
}
