using System;

namespace ErabliereApi.Donnees.Action.Delete;

/// <summary>
/// Modèle de suppression d'un baril. Seuls les ids sont lus : le baril est chargé par le serveur
/// et doit appartenir à l'érablière de la route.
/// </summary>
public class DeleteBaril
{
    /// <summary>
    /// L'id du baril à supprimer
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// L'id de l'érablière du baril, qui doit concorder avec celui de la route
    /// </summary>
    public Guid? IdErabliere { get; set; }
}
