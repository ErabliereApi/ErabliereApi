using System;

namespace ErabliereApi.Donnees.Action.Delete;

/// <summary>
/// Modèle de suppression d'un dompeux. Seuls les ids sont lus : le dompeux est chargé par le serveur
/// et doit appartenir à l'érablière de la route.
/// </summary>
public class DeleteDompeux
{
    /// <summary>
    /// L'id du dompeux à supprimer
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// L'id de l'érablière du dompeux, qui doit concorder avec celui de la route
    /// </summary>
    public Guid? IdErabliere { get; set; }
}
