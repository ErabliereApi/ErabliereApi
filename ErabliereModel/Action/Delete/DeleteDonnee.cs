using System;

namespace ErabliereApi.Donnees.Action.Delete;

/// <summary>
/// Modèle de suppression d'une donnée du trio de données. Seuls les ids sont lus : la donnée est
/// chargée par le serveur et doit appartenir à l'érablière de la route.
/// </summary>
public class DeleteDonnee
{
    /// <summary>
    /// L'id de la donnée à supprimer
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// L'id de l'érablière de la donnée, qui doit concorder avec celui de la route
    /// </summary>
    public Guid? IdErabliere { get; set; }
}
