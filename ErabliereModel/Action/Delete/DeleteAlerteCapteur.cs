using System;

namespace ErabliereApi.Donnees.Action.Delete;

/// <summary>
/// Modèle de suppression d'une alerte de capteur. Seuls les ids sont lus : l'alerte est
/// chargée par le serveur et doit appartenir au capteur de la route.
/// </summary>
public class DeleteAlerteCapteur
{
    /// <summary>
    /// L'id de l'alerte à supprimer
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// L'id du capteur de l'alerte, qui doit concorder avec celui de la route
    /// </summary>
    public Guid? IdCapteur { get; set; }
}
