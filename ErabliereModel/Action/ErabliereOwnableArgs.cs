using System;

namespace ErabliereApi.Action;

/// <summary>
/// Arguments d'action pour les entités possédant une érablière
/// </summary>
public class ErabliereOwnableArgs
{
    /// <summary>
    /// L'id de l'entité
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// L'id de l'érablière
    /// </summary>
    public Guid? IdErabliere { get; set; }
}